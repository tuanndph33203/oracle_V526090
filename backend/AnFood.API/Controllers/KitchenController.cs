using AnFood.Application.Bussiness.KitchenBussiness;
using AnFood.Infrastructure;
using AnFood.API.Services;
using AnFood.Application.Services.Mqtt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AnFood.Persistence.Context;
using AnFood.Domain.Datatables;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AnFood.API.Controllers
{
    [ApiController]
    [Authorize]
    public class KitchenController : ControllerBase
    {
        private readonly IKitchenHandler _kitchenHandler;
        private readonly KitchenPrintDispatcher _printDispatcher;
        private readonly IMqttSpeakerService _mqttSpeakerService;
        private readonly DataContext _db;

        public KitchenController(IKitchenHandler kitchenHandler, KitchenPrintDispatcher printDispatcher, IMqttSpeakerService mqttSpeakerService, DataContext db)
        {
            _kitchenHandler = kitchenHandler;
            _printDispatcher = printDispatcher;
            _mqttSpeakerService = mqttSpeakerService;
            _db = db;
        }

        [HttpPost("/api/sale-orders/{orderId}/send-kitchen")]
        public async Task<Response> SendKitchen([FromRoute] Guid orderId, [FromBody] SendKitchenRequestDto request)
        {
            var response = await _kitchenHandler.SendKitchen(orderId, request);
            if (response.Code == Code.Success && response is ResponseObject<SendKitchenResponseDto> result && result.Data != null)
            {
                if (result.Data.CreatedRequests.Count == 0 && result.Data.PrintSlips.Any(slip => slip.SlipType == "Cancel"))
                    _ = _mqttSpeakerService.NotifyKitchenCancellationAsync(result.Data.PrintSlips.First(slip => slip.SlipType == "Cancel").OrderCode);
                await _printDispatcher.DispatchAsync(request.BranchCode, result.Data, HttpContext.RequestAborted);
            }
            return response;
        }

        [HttpPost("/api/kitchen/dispatch-cancellations")]
        public async Task<Response> DispatchCancellations([FromBody] KitchenCancellationDispatchRequest request, CancellationToken cancellationToken)
        {
            if (request.OrderId == Guid.Empty || string.IsNullOrWhiteSpace(request.BranchCode) || request.Items == null || request.Items.Count == 0)
                return new Response(Code.BadRequest, "Thông tin phiếu hủy món không hợp lệ.");

            var order = await _db.SalesOrders.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.OrderId && x.BranchCode == request.BranchCode, cancellationToken);
            if (order == null) return new Response(Code.NotFound, "Không tìm thấy đơn hàng trong chi nhánh.");

            var requestedQuantities = request.Items
                .Where(x => x.OrderItemId != Guid.Empty && x.Quantity > 0)
                .GroupBy(x => x.OrderItemId)
                .ToDictionary(x => x.Key, x => x.Sum(y => y.Quantity));
            var orderItems = await _db.SalesOrderItems.AsNoTracking()
                .Where(x => x.OrderId == request.OrderId && x.BranchCode == request.BranchCode && requestedQuantities.Keys.Contains(x.Id))
                .ToListAsync(cancellationToken);
            if (orderItems.Count == 0) return new Response(Code.BadRequest, "Không tìm thấy món cần in phiếu hủy.");

            var menuIds = orderItems.Select(x => x.ItemId).Distinct().ToList();
            var menuItems = await _db.MenuItems.AsNoTracking().Where(x => menuIds.Contains(x.Id)).ToListAsync(cancellationToken);
            var groupIds = menuItems.Where(x => x.MenuGroupId.HasValue).Select(x => x.MenuGroupId!.Value).Distinct().ToList();
            var menuGroups = await _db.MenuGroups.AsNoTracking().Where(x => groupIds.Contains(x.Id)).ToListAsync(cancellationToken);
            var defaultAreaSetting = await _db.SystemSettings.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == Constant.SystemSettingCodes.KitchenDefaultAreaId, cancellationToken);
            var defaultAreaValue = defaultAreaSetting?.Value?.Trim();
            var activeAreas = await _db.PreparationAreas.AsNoTracking().Where(x => x.Status == 1).ToListAsync(cancellationToken);
            var defaultArea = activeAreas.FirstOrDefault(x =>
                Guid.TryParse(defaultAreaValue, out var areaId) && x.Id == areaId ||
                string.Equals(x.Code, defaultAreaValue, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.Name, defaultAreaValue, StringComparison.OrdinalIgnoreCase)) ?? activeAreas.FirstOrDefault();
            var areasById = activeAreas.ToDictionary(x => x.Id);
            var menuById = menuItems.ToDictionary(x => x.Id);
            var groupById = menuGroups.ToDictionary(x => x.Id);

            var tableNames = await _db.SalesOrderTables.AsNoTracking()
                .Where(x => x.OrderId == request.OrderId && x.BranchCode == request.BranchCode)
                .OrderBy(x => x.TableName).Select(x => x.TableName).ToListAsync(cancellationToken);
            var tableName = tableNames.Count > 0 ? string.Join(" + ", tableNames) : order.ServiceType == "DineIn" ? "Tại bàn" : order.ServiceType;
            var groupedItems = new Dictionary<Guid, (PreparationArea? Area, List<KitchenPrintItemDto> Items)>();

            foreach (var item in orderItems)
            {
                var menu = menuById.GetValueOrDefault(item.ItemId);
                var group = menu?.MenuGroupId is Guid groupId ? groupById.GetValueOrDefault(groupId) : null;
                var areaId = menu?.PreparationAreaId ?? group?.PreparationAreaId ?? defaultArea?.Id;
                var area = areaId.HasValue ? areasById.GetValueOrDefault(areaId.Value) : defaultArea;
                var key = area?.Id ?? Guid.Empty;
                if (!groupedItems.TryGetValue(key, out var bucket))
                {
                    bucket = (area, new List<KitchenPrintItemDto>());
                    groupedItems[key] = bucket;
                }

                var requestedQty = requestedQuantities[item.Id];
                var quantity = item.SentQty > 0 ? Math.Min(requestedQty, item.SentQty) : requestedQty;
                bucket.Items.Add(new KitchenPrintItemDto
                {
                    MenuItemId = item.ItemId,
                    MenuItemName = item.ItemName,
                    Quantity = quantity,
                    Note = item.Note,
                    Reason = request.Reason
                });
            }

            var now = DateTime.Now;
            var slips = groupedItems.Select(group => new KitchenPrintSlipDto
            {
                SlipType = "Cancel",
                KitchenAreaId = group.Key,
                KitchenAreaName = group.Value.Area?.Name ?? "Bếp / Bar",
                DeviceType = group.Value.Area?.DeviceType ?? 0,
                ProcessingArea = group.Value.Area?.ProcessingArea,
                RequestCode = order.OrderCode + "-CANCEL",
                OrderCode = order.OrderCode,
                TableName = tableName,
                BranchCode = order.BranchCode,
                ServiceType = order.ServiceType,
                WaiterName = order.WaiterName,
                CustomerCount = order.CustomerCount,
                PrintTime = now,
                OrderNote = order.Note,
                Items = group.Value.Items
            }).ToList();

            var result = new SendKitchenResponseDto
            {
                Success = true,
                Message = "Đã xử lý phiếu hủy món.",
                OrderStatus = order.Status,
                PrintSlips = slips
            };
            await _printDispatcher.DispatchAsync(request.BranchCode, result, cancellationToken);
            return new ResponseObject<SendKitchenResponseDto>(result, result.Message);
        }

        [HttpGet("api/kitchen/requests")]
        public async Task<Response> GetRequests([FromQuery] string branchCode, [FromQuery] string? kitchenAreaIds, [FromQuery] Guid? menuItemId)
        {
            return await _kitchenHandler.GetRequests(branchCode, kitchenAreaIds, menuItemId);
        }

        [HttpGet("api/kitchen/summary")]
        public async Task<Response> GetSummary([FromQuery] string branchCode, [FromQuery] string? kitchenAreaIds)
        {
            return await _kitchenHandler.GetSummary(branchCode, kitchenAreaIds);
        }

        [HttpPost("api/kitchen/serve-items")]
        public async Task<Response> ServeItems([FromBody] ServeItemsRequestDto request)
        {
            var response = await _kitchenHandler.ServeItems(request);
            if (response.Code == Code.Success && response is ResponseObject<ServeItemsResponseDto> result && result.Data != null)
            {
                var undispatchedSlips = new List<KitchenPrintSlipDto>();
                foreach (var branchGroup in result.Data.PrintSlips
                             .Where(slip => !string.IsNullOrWhiteSpace(slip.BranchCode))
                             .GroupBy(slip => slip.BranchCode!))
                {
                    var dispatchResponse = new SendKitchenResponseDto { PrintSlips = branchGroup.ToList() };
                    await _printDispatcher.DispatchAsync(branchGroup.Key, dispatchResponse, HttpContext.RequestAborted);
                    result.Data.PrintDispatchResults.AddRange(dispatchResponse.PrintDispatchResults);
                    undispatchedSlips.AddRange(dispatchResponse.PrintSlips);
                }

                result.Data.PrintSlips = undispatchedSlips;
            }
            return response;
        }

        [HttpPost("api/kitchen/sold-out-items")]
        public async Task<Response> SoldOutItems([FromBody] SoldOutItemsRequestDto request)
        {
            var response = await _kitchenHandler.SoldOutItems(request);
            if (response.Code == Code.Success && response is ResponseObject<SoldOutItemsResponseDto> result && result.Data != null)
            {
                var fallbackSlips = new List<KitchenPrintSlipDto>();
                foreach (var branchGroup in result.Data.PrintSlips
                             .Where(slip => !string.IsNullOrWhiteSpace(slip.BranchCode))
                             .GroupBy(slip => slip.BranchCode!))
                {
                    var dispatchResponse = new SendKitchenResponseDto { PrintSlips = branchGroup.ToList() };
                    await _printDispatcher.DispatchAsync(branchGroup.Key, dispatchResponse, HttpContext.RequestAborted);
                    result.Data.PrintDispatchResults.AddRange(dispatchResponse.PrintDispatchResults);
                    fallbackSlips.AddRange(dispatchResponse.PrintSlips);
                }

                result.Data.PrintSlips = fallbackSlips;
            }

            return response;
        }
    }
}
