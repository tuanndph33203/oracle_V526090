using AnFood.Domain.Datatables;
using AnFood.Persistence;
using AnFood.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnFood.Application.Bussiness.AppLogger;
using AnFood.Application.Bussiness.SalesBussiness;
using AnFood.Application.Services.Mqtt;

namespace AnFood.Application.Bussiness.KitchenBussiness
{
    public class KitchenHandler : IKitchenHandler
    {
        private readonly IUnitOfWork _unit;
        private readonly IAppLogger _logger;
        private readonly IKitchenHubSender _hubSender;
        private readonly IMqttSpeakerService _mqttSpeakerService;
        private readonly IMessageGatewayService _messageGateway;

        public KitchenHandler(
            IUnitOfWork unit, 
            IAppLogger logger, 
            IKitchenHubSender hubSender, 
            IMqttSpeakerService mqttSpeakerService,
            IMessageGatewayService messageGateway)
        {
            _unit = unit;
            _logger = logger;
            _hubSender = hubSender;
            _mqttSpeakerService = mqttSpeakerService;
            _messageGateway = messageGateway;
        }

        private static string? KitchenNote(SalesOrderItem item)
        {
            if (string.IsNullOrWhiteSpace(item.SelectedOptionsJson)) return item.Note;
            var options = System.Text.Json.JsonSerializer.Deserialize<List<OrderSelectedOptionDto>>(item.SelectedOptionsJson) ?? new();
            var label = string.Join(" | ", options
                .GroupBy(x => string.IsNullOrWhiteSpace(x.GroupCode) ? x.GroupName : x.GroupCode, StringComparer.OrdinalIgnoreCase)
                .Select(g => $"{g.First().GroupName}: {string.Join(", ", g.Select(x => x.ValueName))}"));
            return string.IsNullOrWhiteSpace(label) ? item.Note
                : string.IsNullOrWhiteSpace(item.Note) ? label : $"{label} | {item.Note}";
        }

        private Task BroadcastSalesEventAsync(string branchCode, SalesRealtimeEventDto eventData)
        {
            return Task.CompletedTask;
        }

        private async Task<bool> IsRequireKitchenSendEnabled()
        {
            var setting = await _unit.GetRepository<SystemSetting>()
                .Get(x => x.Code.ToLower() == Constant.SystemSettingCodes.SaleRequireKitchen.ToLower())
                .FirstOrDefaultAsync();
            if (setting == null)
                return true;
            return Constant.SystemSettingAllowsAction(setting.Value) && setting.IsActive;
        }

        private async Task<bool> IsKitchenPrintEnabled()
        {
            var setting = await _unit.GetRepository<SystemSetting>()
                .Get(x => x.Code.ToLower() == Constant.SystemSettingCodes.SaleEnableKitchenPrint.ToLower())
                .FirstOrDefaultAsync();
            return setting != null && setting.IsActive && Constant.SystemSettingAllowsAction(setting.Value);
        }

        private async Task<List<PreparationArea>> GetDefaultPreparationAreasAsync()
        {
            var prepAreaRepo = _unit.GetRepository<PreparationArea>();
            var defaultSetting = await _unit.GetRepository<SystemSetting>()
                .Get(x => x.Code.ToLower() == Constant.SystemSettingCodes.KitchenDefaultAreaId.ToLower())
                .FirstOrDefaultAsync();

            if (defaultSetting != null && !string.IsNullOrWhiteSpace(defaultSetting.Value))
            {
                var val = defaultSetting.Value.Trim();
                var parts = val.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var guidList = new List<Guid>();
                var nameOrCodeList = new List<string>();

                foreach (var p in parts)
                {
                    if (Guid.TryParse(p, out var g))
                        guidList.Add(g);
                    else
                        nameOrCodeList.Add(p);
                }

                var matchedAreas = await prepAreaRepo.Get(x => (guidList.Contains(x.Id) || nameOrCodeList.Contains(x.Code) || nameOrCodeList.Contains(x.Name)) && x.Status == 1).ToListAsync();
                if (matchedAreas.Any()) return matchedAreas;
            }

            var fallback = await prepAreaRepo.Get(x => x.Status == 1).FirstOrDefaultAsync()
                ?? await prepAreaRepo.Get(x => x.Status != 0).FirstOrDefaultAsync();

            return fallback != null ? new List<PreparationArea> { fallback } : new List<PreparationArea>();
        }

        private async Task<PreparationArea?> GetDefaultPreparationAreaAsync()
        {
            var list = await GetDefaultPreparationAreasAsync();
            return list.FirstOrDefault();
        }

        private async Task<Dictionary<Guid, PreparationArea>> ResolveEffectivePreparationAreas(
            IEnumerable<Guid> menuItemIds,
            PreparationArea? defaultPrepArea)
        {
            var result = new Dictionary<Guid, PreparationArea>();
            var menuItemRepo = _unit.GetRepository<MenuItem>();
            var menuGroupRepo = _unit.GetRepository<MenuGroup>();
            var prepAreaRepo = _unit.GetRepository<PreparationArea>();

            var menuItems = await menuItemRepo.Get(x => menuItemIds.Contains(x.Id)).ToListAsync();
            var menuGroupIds = menuItems.Where(x => x.MenuGroupId.HasValue).Select(x => x.MenuGroupId!.Value).Distinct().ToList();
            var menuGroups = await menuGroupRepo.Get(x => menuGroupIds.Contains(x.Id)).ToListAsync();
            var menuGroupDict = menuGroups.ToDictionary(x => x.Id, x => x);

            var prepAreaIds = new HashSet<Guid>();
            foreach (var mi in menuItems)
            {
                if (mi.PreparationAreaId.HasValue)
                    prepAreaIds.Add(mi.PreparationAreaId.Value);
                else if (mi.MenuGroupId.HasValue && menuGroupDict.TryGetValue(mi.MenuGroupId.Value, out var mg) && mg.PreparationAreaId.HasValue)
                    prepAreaIds.Add(mg.PreparationAreaId.Value);
            }

            if (defaultPrepArea != null)
                prepAreaIds.Add(defaultPrepArea.Id);

            var prepAreas = await prepAreaRepo.Get(x => prepAreaIds.Contains(x.Id)).ToListAsync();
            var prepAreaDict = prepAreas.ToDictionary(x => x.Id, x => x);

            foreach (var mi in menuItems)
            {
                PreparationArea? matched = null;
                if (mi.PreparationAreaId.HasValue && prepAreaDict.TryGetValue(mi.PreparationAreaId.Value, out var pa1))
                {
                    matched = pa1;
                }
                else if (mi.MenuGroupId.HasValue && menuGroupDict.TryGetValue(mi.MenuGroupId.Value, out var mg) && mg.PreparationAreaId.HasValue && prepAreaDict.TryGetValue(mg.PreparationAreaId.Value, out var pa2))
                {
                    matched = pa2;
                }
                else if (defaultPrepArea != null)
                {
                    matched = defaultPrepArea;
                }

                if (matched != null)
                {
                    result[mi.Id] = matched;
                }
            }

            return result;
        }

        private async Task<string> ResolveTableNameAsync(SalesOrder order, Guid orderId)
        {
            if (string.Equals(order.ServiceType, "DineIn", StringComparison.OrdinalIgnoreCase))
            {
                var orderTables = await _unit.GetRepository<SalesOrderTable>()
                    .Get(x => x.OrderId == orderId)
                    .ToListAsync();
                if (orderTables != null && orderTables.Any())
                {
                    var tableIds = orderTables.Select(ot => ot.TableId).Distinct().ToList();
                    var diningTables = await _unit.GetRepository<DiningTable>().Get(dt => tableIds.Contains(dt.Id)).ToListAsync();
                    var floorPlanIds = diningTables.Select(dt => dt.FloorPlanId).Distinct().ToList();
                    var floors = await _unit.GetRepository<FloorPlan>().Get(fp => floorPlanIds.Contains(fp.Id)).ToListAsync();
                    var floorDict = floors.ToDictionary(f => f.Id, f => f.Name);
                    var dtDict = diningTables.ToDictionary(dt => dt.Id, dt => dt);

                    var tableStrings = new List<string>();
                    foreach (var ot in orderTables.OrderBy(ot => ot.TableName))
                    {
                        var parts = new List<string> { ot.TableName };
                        if (dtDict.TryGetValue(ot.TableId, out var dt) && floorDict.TryGetValue(dt.FloorPlanId, out var floorName) && !string.IsNullOrWhiteSpace(floorName))
                        {
                            parts.Add($"Sàn: {floorName}");
                        }
                        if (!string.IsNullOrWhiteSpace(order.BranchCode))
                        {
                            parts.Add(order.BranchCode);
                        }
                        tableStrings.Add(string.Join(" - ", parts));
                    }
                    return string.Join(" + ", tableStrings);
                }
                return string.IsNullOrWhiteSpace(order.BranchCode) ? "Tại bàn" : $"Tại bàn - {order.BranchCode}";
            }
            if (string.Equals(order.ServiceType, "TakeAway", StringComparison.OrdinalIgnoreCase))
            {
                return string.IsNullOrWhiteSpace(order.BranchCode) ? "Mang về" : $"Mang về - {order.BranchCode}";
            }
            if (string.Equals(order.ServiceType, "Delivery", StringComparison.OrdinalIgnoreCase))
            {
                return string.IsNullOrWhiteSpace(order.BranchCode) ? "Giao hàng" : $"Giao hàng - {order.BranchCode}";
            }
            return string.IsNullOrWhiteSpace(order.BranchCode) ? "Yêu cầu" : $"Yêu cầu - {order.BranchCode}";
        }

        public async Task<Response> SendKitchen(Guid orderId, SendKitchenRequestDto request)
        {
            try
            {
                if (request == null)
                    return new Response(Code.BadRequest, "Yêu cầu không hợp lệ!");

                var now = DateTime.Now;

                var orderRepo = _unit.GetRepository<SalesOrder>();
                var itemRepo = _unit.GetRepository<SalesOrderItem>();
                var kitchenRequestRepo = _unit.GetRepository<KitchenRequest>();
                var kitchenRequestDetailRepo = _unit.GetRepository<KitchenRequestDetail>();

                // 1. Check order existence
                var order = await orderRepo.Get(x => x.Id == orderId && x.BranchCode == request.BranchCode).FirstOrDefaultAsync();
                if (order == null)
                    return new Response(Code.NotFound, "Hóa đơn không tồn tại!");

                if (order.Status == "Paid" || order.Status == "Cancelled")
                    return new Response(Code.BadRequest, "Hóa đơn đã thanh toán hoặc đã hủy, không thể gửi bếp!");

                // 2. Fetch order items
                var items = await itemRepo.Get(x => x.OrderId == orderId && x.BranchCode == request.BranchCode && x.IsCancelled == 0).ToListAsync();

                var defaultPrepArea = await GetDefaultPreparationAreaAsync();
                var allMenuItemIds = items.Select(x => x.ItemId).Distinct().ToList();
                var effectivePrepAreaByItemId = await ResolveEffectivePreparationAreas(allMenuItemIds, defaultPrepArea);

                var tableName = await ResolveTableNameAsync(order, orderId);
                var sendBy = request.SendBy ?? order.WaiterName ?? "System";

                bool requireKitchen = await IsRequireKitchenSendEnabled();
                if (!requireKitchen)
                {
                    // Khi tắt setting bắt buộc gửi bếp: chỉ duyệt/lưu đơn sang trạng thái hoạt động (Serving), đánh dấu món đã hoàn tất phục vụ, KHÔNG gửi lệnh sang Bếp/Bar
                    var printSlipsForNoKitchen = new List<KitchenPrintSlipDto>();
                    var unsentForSlip = items.Where(x => x.Quantity > x.SentQty).ToList();
                    var reducedForSlip = items.Where(x => x.Quantity < x.SentQty).ToList();
                    foreach (var group in reducedForSlip.GroupBy(x =>
                                 effectivePrepAreaByItemId.TryGetValue(x.ItemId, out var area)
                                     ? area.Id
                                     : (defaultPrepArea?.Id ?? Guid.Empty)))
                    {
                        var area = effectivePrepAreaByItemId.Values.FirstOrDefault(x => x.Id == group.Key) ?? defaultPrepArea;
                        printSlipsForNoKitchen.Add(new KitchenPrintSlipDto
                        {
                            SlipType = "Cancel",
                            KitchenAreaId = area?.Id ?? Guid.Empty,
                            KitchenAreaName = area?.Name ?? "Bếp/Bar",
                            DeviceType = area?.DeviceType ?? 2,
                            ProcessingArea = area?.ProcessingArea,
                            RequestCode = order.OrderCode,
                            OrderCode = order.OrderCode,
                            TableName = tableName,
                            BranchCode = order.BranchCode,
                            ServiceType = order.ServiceType,
                            WaiterName = order.WaiterName,
                            CustomerCount = order.CustomerCount,
                            PrintTime = now,
                            OrderNote = order.Note,
                            Items = group.Select(item => new KitchenPrintItemDto
                            {
                                MenuItemId = item.ItemId,
                                MenuItemName = item.ItemName,
                                Quantity = item.SentQty - item.Quantity,
                                Note = KitchenNote(item),
                                Reason = "Hủy / Giảm số lượng món"
                            }).ToList()
                        });
                    }

                    if (unsentForSlip.Any())
                    {
                        var groupedByArea = unsentForSlip.GroupBy(x => effectivePrepAreaByItemId.TryGetValue(x.ItemId, out var pa) ? pa.Id : (defaultPrepArea?.Id ?? Guid.Empty));
                        foreach (var g in groupedByArea)
                        {
                            var area = effectivePrepAreaByItemId.Values.FirstOrDefault(a => a.Id == g.Key) ?? defaultPrepArea;
                            printSlipsForNoKitchen.Add(new KitchenPrintSlipDto
                            {
                                SlipType = "Kitchen",
                                KitchenAreaId = area?.Id ?? Guid.Empty,
                                KitchenAreaName = area?.Name ?? "Bếp/Bar",
                                DeviceType = area?.DeviceType ?? 2,
                                ProcessingArea = area?.ProcessingArea,
                                RequestCode = order.OrderCode,
                                OrderCode = order.OrderCode,
                                TableName = tableName,
                                BranchCode = order.BranchCode,
                                ServiceType = order.ServiceType,
                                WaiterName = order.WaiterName,
                                CustomerCount = order.CustomerCount,
                                PrintTime = now,
                                OrderNote = order.Note,
                                Items = g.Select(i => new KitchenPrintItemDto
                                {
                                    MenuItemId = i.ItemId,
                                    MenuItemName = i.ItemName,
                                    Quantity = i.Quantity - i.SentQty,
                                    Note = KitchenNote(i)
                                }).ToList()
                            });
                        }
                    }

                    using var noKitchenTrans = await _unit.BeginTransactionAsync();
                    foreach (var item in items)
                    {
                        item.SentQty = item.Quantity;
                        item.ServedQty = item.Quantity;
                        item.RemainingQty = 0;
                        item.KitchenStatus = "FullyServed";
                        item.IsSentToKitchen = 1;
                        item.UpdatedOnDate = now;
                        itemRepo.Update(item);
                    }

                    var prevStatus = order.Status;
                    var orderStatusChanged = false;
                    if (string.Equals(order.Status, "Draft", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(order.Status, "WaitingService", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(order.Status, "Reserved", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(order.Status, "WaitingConfirm", StringComparison.OrdinalIgnoreCase))
                    {
                        order.Status = "Serving";
                        order.UpdatedOnDate = now;
                        orderRepo.Update(order);
                        orderStatusChanged = true;
                    }

                    await _unit.SaveAsync();
                    await noKitchenTrans.CommitAsync();

                    if (unsentForSlip.Any() && await IsKitchenPrintEnabled())
                    {
                        var areaTypes = unsentForSlip
                            .Where(item => effectivePrepAreaByItemId.ContainsKey(item.ItemId))
                            .Select(item => effectivePrepAreaByItemId[item.ItemId].Type)
                            .Distinct()
                            .ToArray();
                        await _mqttSpeakerService.NotifyKitchenOrderSpeakerAsync(areaTypes);
                        await _messageGateway.SendAsync($"{DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)):HH:mm:ss} {order.OrderCode}: Có phiếu báo món gửi bếp");
                    }

                    await BroadcastSalesEventAsync(request.BranchCode, new SalesRealtimeEventDto
                    {
                        EventType = "OrderConfirmed",
                        OrderId = order.Id,
                        OrderCode = order.OrderCode,
                        NewStatus = order.Status,
                        ServiceType = order.ServiceType,
                        TableName = tableName
                    });

                    if (orderStatusChanged)
                    {
                        await _logger.InfoAsync($"Order status changed: OrderId={orderId}, BranchCode={request.BranchCode}, Action=SendKitchenBar, OldStatus={prevStatus}, NewStatus=Serving");
                    }

                    return new ResponseObject<SendKitchenResponseDto>
                    {
                        Code = Code.Success,
                        Message = "Đã duyệt và lưu đơn thành công!",
                        Data = new SendKitchenResponseDto
                        {
                            Success = true,
                            Message = "Đã duyệt và lưu đơn thành công",
                            OrderStatus = order.Status,
                            CreatedRequests = new List<CreatedRequestDto>(),
                            PrintSlips = printSlipsForNoKitchen
                        }
                    };
                }

                using var trans = await _unit.BeginTransactionAsync();

                var printSlips = new List<KitchenPrintSlipDto>();

                // 3. Filter unsent items and handle decreased quantities
                var unsentItems = new List<SalesOrderItem>();
                var decreasedItemsWithDiff = new List<(SalesOrderItem Item, double Diff, PreparationArea Area)>();

                foreach (var item in items)
                {
                    if (item.Quantity > item.SentQty)
                    {
                        unsentItems.Add(item);
                    }
                    else if (item.Quantity < item.SentQty)
                    {
                        // Handle quantity decrease
                        var diff = item.SentQty - item.Quantity;
                        item.SentQty = item.Quantity;
                        item.RemainingQty = item.Quantity - item.ServedQty - item.SoldOutQty;
                        if (item.RemainingQty < 0) item.RemainingQty = 0;

                        if (item.ServedQty >= item.Quantity)
                            item.KitchenStatus = "FullyServed";
                        else if (item.ServedQty > 0)
                            item.KitchenStatus = "PartiallyServed";
                        else if (item.SoldOutQty >= item.Quantity)
                            item.KitchenStatus = "FullySoldOut";
                        else if (item.SoldOutQty > 0)
                            item.KitchenStatus = "PartiallySoldOut";
                        else if (item.SentQty > 0)
                            item.KitchenStatus = "Sent";
                        else
                            item.KitchenStatus = "NotSent";

                        item.UpdatedOnDate = now;
                        itemRepo.Update(item);

                        var targetArea = effectivePrepAreaByItemId.TryGetValue(item.ItemId, out var pa) ? pa : defaultPrepArea;
                        if (targetArea != null)
                        {
                            decreasedItemsWithDiff.Add((item, diff, targetArea));
                        }

                        // Find request details to reduce starting from latest
                        var details = await kitchenRequestDetailRepo.Get(x => x.OrderDetailId == item.Id).ToListAsync();
                        var detailsWithHeaders = new List<(KitchenRequestDetail Detail, KitchenRequest Header)>();
                        foreach (var det in details)
                        {
                            var header = await kitchenRequestRepo.Get(x => x.Id == det.KitchenRequestId).FirstOrDefaultAsync();
                            if (header != null)
                            {
                                detailsWithHeaders.Add((det, header));
                            }
                        }

                        // Order by RequestedAt descending (latest first)
                        var detailsSorted = detailsWithHeaders
                            .OrderByDescending(x => x.Header.RequestedAt)
                            .Select(x => x.Detail)
                            .ToList();

                        var remainingDiff = diff;
                        foreach (var det in detailsSorted)
                        {
                            if (remainingDiff <= 0) break;

                            var reduceable = det.RequestQty - det.ServedQty - det.SoldOutQty;
                            if (reduceable > 0)
                            {
                                var reduceAmount = Math.Min(remainingDiff, reduceable);
                                det.RequestQty -= reduceAmount;
                                det.RemainingQty = det.RequestQty - det.ServedQty - det.SoldOutQty;
                                if (det.RemainingQty < 0) det.RemainingQty = 0;

                                if (det.ServedQty + det.SoldOutQty == det.RequestQty)
                                {
                                    det.Status = "Completed";
                                }
                                kitchenRequestDetailRepo.Update(det);
                                remainingDiff -= reduceAmount;
                            }
                        }
                    }
                }

                // Generate Cancellation Print Slips for decreased items
                if (decreasedItemsWithDiff.Any())
                {
                    var cancelGroupByArea = decreasedItemsWithDiff.GroupBy(x => x.Area.Id);
                    foreach (var cg in cancelGroupByArea)
                    {
                        var area = cg.First().Area;
                        var cancelDesc = string.Join(", ", cg.Select(x => $"{x.Item.ItemName} (-{x.Diff})"));
                        printSlips.Add(new KitchenPrintSlipDto
                        {
                            SlipType = "Cancel",
                            KitchenAreaId = area.Id,
                            KitchenAreaName = area.Name,
                            DeviceType = area.DeviceType,
                            ProcessingArea = area.ProcessingArea,
                            RequestCode = order.OrderCode,
                            OrderCode = order.OrderCode,
                            TableName = tableName,
                            BranchCode = order.BranchCode,
                            ServiceType = order.ServiceType,
                            WaiterName = order.WaiterName,
                            CustomerCount = order.CustomerCount,
                            PrintTime = now,
                            OrderNote = order.Note,
                            Items = cg.Select(x => new KitchenPrintItemDto
                            {
                                MenuItemId = x.Item.ItemId,
                                MenuItemName = x.Item.ItemName,
                                Quantity = x.Diff,
                                Note = KitchenNote(x.Item),
                                Reason = "Hủy / Giảm số lượng món"
                            }).ToList()
                        });
                    }
                }

                // If only decreased quantities occurred and no new items to send
                if (!unsentItems.Any())
                {
                    await _unit.SaveAsync();
                    await trans.CommitAsync();
                    
                    // Broadcast requests updated to refresh kitchen UI
                    await _hubSender.SendRequestsUpdatedAsync(request.BranchCode);

                    return new ResponseObject<SendKitchenResponseDto>
                    {
                        Code = Code.Success,
                        Message = "Cập nhật số lượng Bếp/Bar thành công",
                        Data = new SendKitchenResponseDto
                        {
                            Success = true,
                            Message = "Cập nhật số lượng Bếp/Bar thành công",
                            OrderStatus = order.Status,
                            CreatedRequests = new List<CreatedRequestDto>(),
                            PrintSlips = printSlips
                        }
                    };
                }

                // 4. Validate Preparation Areas for all unsent items
                foreach (var item in unsentItems)
                {
                    if (!effectivePrepAreaByItemId.ContainsKey(item.ItemId) && defaultPrepArea != null)
                    {
                        effectivePrepAreaByItemId[item.ItemId] = defaultPrepArea;
                    }

                    if (!effectivePrepAreaByItemId.ContainsKey(item.ItemId))
                    {
                        return new Response(Code.BadRequest, $"Món '{item.ItemName}' chưa cấu hình khu vực chế biến!");
                    }
                }

                // 5. Calculate sequence for kitchen request code
                var nextRequestSeq = await GetNextKitchenRequestSequence(request.BranchCode, order.OrderCode);

                var createdRequests = new List<CreatedRequestDto>();

                // Group unsent items by effective PreparationArea
                var itemsByArea = unsentItems.GroupBy(x => effectivePrepAreaByItemId[x.ItemId].Id);

                foreach (var group in itemsByArea)
                {
                    var areaId = group.Key;
                    var area = effectivePrepAreaByItemId.Values.First(x => x.Id == areaId);

                    // Create KitchenRequest
                    var requestCode = $"{order.OrderCode}-{nextRequestSeq:D3}";
                    nextRequestSeq++;

                    var kitchenRequest = new KitchenRequest
                    {
                        Id = Guid.NewGuid(),
                        RequestCode = requestCode,
                        OrderId = orderId,
                        OrderCode = order.OrderCode,
                        BranchCode = request.BranchCode,
                        KitchenAreaId = areaId,
                        ServiceType = order.ServiceType,
                        TableId = request.TableId,
                        TableName = tableName,
                        RequestedAt = now,
                        RequestedBy = sendBy,
                        Status = "Pending"
                    };

                    kitchenRequestRepo.Add(kitchenRequest);

                    var slipItems = new List<KitchenPrintItemDto>();

                    // Create Details
                    foreach (var item in group)
                    {
                        var delta = item.Quantity - item.SentQty;
                        var detail = new KitchenRequestDetail
                        {
                            Id = Guid.NewGuid(),
                            KitchenRequestId = kitchenRequest.Id,
                            OrderDetailId = item.Id,
                            MenuItemId = item.ItemId,
                            MenuItemName = item.ItemName,
                            RequestQty = delta,
                            ServedQty = 0,
                            SoldOutQty = 0,
                            RemainingQty = delta,
                            Status = "Pending"
                        };
                        kitchenRequestDetailRepo.Add(detail);

                        slipItems.Add(new KitchenPrintItemDto
                        {
                            MenuItemId = item.ItemId,
                            MenuItemName = item.ItemName,
                            Quantity = delta,
                            Note = KitchenNote(item)
                        });

                        // Update SalesOrderItem
                        item.SentQty += delta;
                        item.IsSentToKitchen = 1;
                        item.KitchenStatus = "Sent";
                        item.RemainingQty = item.Quantity - item.ServedQty - item.SoldOutQty;
                        item.UpdatedOnDate = now;
                        itemRepo.Update(item);
                    }

                    createdRequests.Add(new CreatedRequestDto
                    {
                        RequestId = kitchenRequest.Id,
                        RequestCode = requestCode,
                        KitchenAreaId = areaId,
                        KitchenAreaName = area.Name
                    });

                    printSlips.Add(new KitchenPrintSlipDto
                    {
                        SlipType = "Kitchen",
                        KitchenAreaId = areaId,
                        KitchenAreaName = area.Name,
                        DeviceType = area.DeviceType,
                        ProcessingArea = area.ProcessingArea,
                        RequestCode = requestCode,
                        OrderCode = order.OrderCode,
                        TableName = tableName,
                        BranchCode = order.BranchCode,
                        ServiceType = order.ServiceType,
                        WaiterName = order.WaiterName,
                        CustomerCount = order.CustomerCount,
                        PrintTime = now,
                        OrderNote = order.Note,
                        Items = slipItems
                    });
                }

                // 6. Update order status if necessary
                var oldStatus = order.Status;
                var statusChanged = false;
                if (string.Equals(order.Status, "Draft", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(order.Status, "WaitingService", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(order.Status, "Reserved", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(order.Status, "WaitingConfirm", StringComparison.OrdinalIgnoreCase))
                {
                    order.Status = "Serving";
                    order.UpdatedOnDate = now;
                    orderRepo.Update(order);
                    statusChanged = true;
                }

                await _unit.SaveAsync();
                await trans.CommitAsync();

                // Notify only after the kitchen requests have been committed.
                var notifiedAreaTypes = unsentItems
                    .Select(item => effectivePrepAreaByItemId[item.ItemId].Type)
                    .Distinct()
                    .ToArray();
                await _mqttSpeakerService.NotifyKitchenOrderAsync(tableName, "", (int)unsentItems.Sum(x => x.Quantity), notifiedAreaTypes, order.OrderCode);

                await BroadcastSalesEventAsync(request.BranchCode, new SalesRealtimeEventDto
                {
                    EventType = "OrderConfirmed",
                    OrderId = order.Id,
                    OrderCode = order.OrderCode,
                    NewStatus = order.Status,
                    ServiceType = order.ServiceType,
                    TableName = tableName
                });

                if (statusChanged)
                {
                    await _logger.InfoAsync($"Order status changed via Kitchen Send: OrderId={orderId}, BranchCode={request.BranchCode}, OldStatus={oldStatus}, NewStatus=Serving");
                }

                // 7. Broadcast new request event via SignalR
                foreach (var reqDto in createdRequests)
                {
                    var fullReq = await GetSingleRequestDto(reqDto.RequestId);
                    if (fullReq != null)
                    {
                        await _hubSender.SendNewRequestAsync(request.BranchCode, fullReq);
                    }
                }

                return new ResponseObject<SendKitchenResponseDto>
                {
                    Code = Code.Success,
                    Message = "Gửi Bếp/Bar thành công",
                    Data = new SendKitchenResponseDto
                    {
                        Success = true,
                        Message = "Gửi Bếp/Bar thành công",
                        OrderStatus = order.Status,
                        CreatedRequests = createdRequests,
                        PrintSlips = printSlips
                    }
                };
            }
            catch (Exception ex)
            {
                var detailMessage = ex.InnerException != null ? $"{ex.Message} -> {ex.InnerException.Message}" : ex.Message;
                await _logger.ErrorAsync("KitchenHandler.SendKitchen error", ex);
                return new Response(Code.ServerError, "Lỗi server: " + detailMessage);
            }
        }

        public async Task<Response> GetRequests(string branchCode, string? kitchenAreaIds, Guid? menuItemId)
        {
            try
            {
                var kitchenRequestRepo = _unit.GetRepository<KitchenRequest>();
                var kitchenRequestDetailRepo = _unit.GetRepository<KitchenRequestDetail>();
                var prepAreaRepo = _unit.GetRepository<PreparationArea>();

                var areas = await prepAreaRepo.Get(x => x.Status == 1).ToListAsync();
                var areaDict = areas.ToDictionary(x => x.Id, x => x.Name);

                // Fetch active requests (Status != Completed and Status != Cancelled)
                var query = kitchenRequestRepo.Get(x => x.BranchCode == branchCode && x.Status != "Completed" && x.Status != "Cancelled");

                if (!string.IsNullOrWhiteSpace(kitchenAreaIds))
                {
                    var areaIdList = kitchenAreaIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToList();
                    query = query.Where(x => areaIdList.Contains(x.KitchenAreaId));
                }

                var requests = await query.OrderBy(x => x.RequestedAt).ToListAsync();
                var requestIds = requests.Select(x => x.Id).ToList();

                var detailsQuery = kitchenRequestDetailRepo.Get(x => requestIds.Contains(x.KitchenRequestId) && x.RemainingQty > 0);
                if (menuItemId.HasValue && menuItemId.Value != Guid.Empty)
                {
                    detailsQuery = detailsQuery.Where(x => x.MenuItemId == menuItemId.Value);
                }

                var orderItemRepo = _unit.GetRepository<SalesOrderItem>();
                var detailsWithNote = await (from d in detailsQuery
                                             join oi in orderItemRepo.Get(x => true) on d.OrderDetailId equals oi.Id into ois
                                             from oi in ois.DefaultIfEmpty()
                                             select new {
                                                 Detail = d,
                                                 OrderItem = oi
                                             }).ToListAsync();
                var detailsGrouped = detailsWithNote.GroupBy(x => x.Detail.KitchenRequestId).ToDictionary(x => x.Key, x => x.ToList());

                // Fetch corresponding SalesOrders to get FloorName
                var orderIds = requests.Select(x => x.OrderId).Distinct().ToList();
                var orderRepo = _unit.GetRepository<SalesOrder>();
                var orders = await orderRepo.Get(x => orderIds.Contains(x.Id)).ToListAsync();
                var orderDict = orders.ToDictionary(x => x.Id, x => x);

                var result = new List<KitchenRequestDto>();
                var now = DateTime.Now;

                foreach (var req in requests)
                {
                    if (!detailsGrouped.TryGetValue(req.Id, out var reqDetails) || !reqDetails.Any())
                        continue;

                    orderDict.TryGetValue(req.OrderId, out var orderObj);
                    var floorName = orderObj?.FloorName;

                    result.Add(new KitchenRequestDto
                    {
                        RequestId = req.Id,
                        RequestCode = req.RequestCode,
                        OrderCode = req.OrderCode,
                        ServiceType = req.ServiceType,
                        TableName = req.TableName ?? "Bàn",
                        FloorName = floorName,
                        RequestedAt = req.RequestedAt,
                        ElapsedSeconds = (now - req.RequestedAt).TotalSeconds,
                        KitchenAreaName = areaDict.TryGetValue(req.KitchenAreaId, out var areaName) ? areaName : "Bếp",
                        Details = reqDetails.Select(d => new KitchenRequestDetailDto
                        {
                            RequestDetailId = d.Detail.Id,
                            MenuItemId = d.Detail.MenuItemId,
                            MenuItemName = d.Detail.MenuItemName,
                            RequestQty = d.Detail.RequestQty,
                            ServedQty = d.Detail.ServedQty,
                            SoldOutQty = d.Detail.SoldOutQty,
                            RemainingQty = d.Detail.RemainingQty,
                            Note = d.OrderItem != null ? KitchenNote(d.OrderItem) : null
                        }).ToList()
                    });
                }

                return new ResponseObject<List<KitchenRequestDto>>
                {
                    Code = Code.Success,
                    Message = "Lấy danh sách yêu cầu thành công",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("KitchenHandler.GetRequests error", ex);
                return new Response(Code.ServerError, "Lỗi: " + ex.Message);
            }
        }

        public async Task<Response> GetSummary(string branchCode, string? kitchenAreaIds)
        {
            try
            {
                var kitchenRequestRepo = _unit.GetRepository<KitchenRequest>();
                var kitchenRequestDetailRepo = _unit.GetRepository<KitchenRequestDetail>();

                var requestQuery = kitchenRequestRepo.Get(x => x.BranchCode == branchCode && x.Status != "Completed" && x.Status != "Cancelled");

                if (!string.IsNullOrWhiteSpace(kitchenAreaIds))
                {
                    var areaIdList = kitchenAreaIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToList();
                    requestQuery = requestQuery.Where(x => areaIdList.Contains(x.KitchenAreaId));
                }

                var requestIds = await requestQuery.Select(x => x.Id).ToListAsync();

                var details = await kitchenRequestDetailRepo.Get(x => requestIds.Contains(x.KitchenRequestId) && x.RemainingQty > 0).ToListAsync();

                var summary = details.GroupBy(x => new { x.MenuItemId, x.MenuItemName })
                    .Select(g => new KitchenSummaryDto
                    {
                        MenuItemId = g.Key.MenuItemId,
                        MenuItemName = g.Key.MenuItemName,
                        RemainingQty = g.Sum(x => x.RemainingQty)
                    })
                    .OrderBy(x => x.MenuItemName)
                    .ToList();

                return new ResponseObject<List<KitchenSummaryDto>>
                {
                    Code = Code.Success,
                    Message = "Lấy tổng hợp món cần làm thành công",
                    Data = summary
                };
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("KitchenHandler.GetSummary error", ex);
                return new Response(Code.ServerError, "Lỗi: " + ex.Message);
            }
        }

        public async Task<Response> ServeItems(ServeItemsRequestDto request)
        {
            try
            {
                if (request == null || request.Items == null || !request.Items.Any())
                    return new Response(Code.BadRequest, "Danh sách món trả không được để trống!");

                var detailRepo = _unit.GetRepository<KitchenRequestDetail>();
                var requestRepo = _unit.GetRepository<KitchenRequest>();
                var itemRepo = _unit.GetRepository<SalesOrderItem>();
                var orderRepo = _unit.GetRepository<SalesOrder>();
                var preparationAreaRepo = _unit.GetRepository<PreparationArea>();

                using var trans = await _unit.BeginTransactionAsync();
                var printJobs = new List<PrintJobDto>();
                var serveSlipsByRequest = new Dictionary<Guid, KitchenPrintSlipDto>();
                var affectedRequests = new HashSet<Guid>();
                var affectedBranches = new HashSet<string>();

                foreach (var item in request.Items)
                {
                    if (item.ServeQty <= 0)
                        return new Response(Code.BadRequest, "Số lượng trả phải lớn hơn 0!");

                    var detail = await detailRepo.Get(x => x.Id == item.RequestDetailId).FirstOrDefaultAsync();
                    if (detail == null)
                        return new Response(Code.NotFound, $"Không tìm thấy chi tiết yêu cầu với Id {item.RequestDetailId}!");

                    if (item.ServeQty > detail.RemainingQty)
                        return new Response(Code.BadRequest, $"Số lượng trả ({item.ServeQty}) vượt quá số lượng còn lại ({detail.RemainingQty}) của món '{detail.MenuItemName}'!");

                    var orderItem = await itemRepo.Get(x => x.Id == detail.OrderDetailId).FirstOrDefaultAsync();
                    var parentRequest = await requestRepo.Get(x => x.Id == detail.KitchenRequestId).FirstOrDefaultAsync();
                    if (parentRequest != null)
                    {
                        affectedRequests.Add(parentRequest.Id);
                        affectedBranches.Add(parentRequest.BranchCode);
                        if (!printJobs.Any(x => x.RequestCode == parentRequest.RequestCode))
                        {
                            printJobs.Add(new PrintJobDto
                            {
                                RequestCode = parentRequest.RequestCode,
                                PrintType = "ServeItem"
                            });
                        }
                        if (!serveSlipsByRequest.TryGetValue(parentRequest.Id, out var serveSlip))
                        {
                            var area = await preparationAreaRepo.Get(x => x.Id == parentRequest.KitchenAreaId).FirstOrDefaultAsync();
                            var order = await orderRepo.Get(x => x.Id == parentRequest.OrderId).FirstOrDefaultAsync();
                            serveSlip = new KitchenPrintSlipDto
                            {
                                SlipType = "Serve",
                                KitchenAreaId = parentRequest.KitchenAreaId,
                                KitchenAreaName = area?.Name ?? "Bếp / Bar",
                                DeviceType = area?.DeviceType ?? 2,
                                ProcessingArea = area?.ProcessingArea,
                                RequestCode = parentRequest.RequestCode + "-SERVE",
                                OrderCode = parentRequest.OrderCode,
                                TableName = parentRequest.TableName,
                                BranchCode = parentRequest.BranchCode,
                                ServiceType = parentRequest.ServiceType,
                                WaiterName = order?.WaiterName,
                                CustomerCount = order?.CustomerCount ?? 0,
                                PrintTime = DateTime.Now,
                                OrderNote = order?.Note
                            };
                            serveSlipsByRequest[parentRequest.Id] = serveSlip;
                        }
                        serveSlip.Items.Add(new KitchenPrintItemDto
                        {
                            MenuItemId = detail.MenuItemId,
                            MenuItemName = detail.MenuItemName,
                            Quantity = item.ServeQty,
                            Note = orderItem != null ? KitchenNote(orderItem) : null
                        });
                        // Mock printing ticket for served item
                        await _logger.InfoAsync($"[PRINT TICKET - TRẢ MÓN] Bàn/Khu vực: {parentRequest.TableName}, Món: {detail.MenuItemName}, Số lượng trả: {item.ServeQty}, Mã yêu cầu: {parentRequest.RequestCode}");
                    }

                    // Update detail quantities
                    detail.ServedQty += item.ServeQty;
                    detail.RemainingQty -= item.ServeQty;
                    if (detail.ServedQty + detail.SoldOutQty == detail.RequestQty)
                    {
                        detail.Status = "Completed";
                    }
                    else
                    {
                        detail.Status = "PartialServed";
                    }
                    detailRepo.Update(detail);

                    // Update corresponding SalesOrderItem
                    if (orderItem != null)
                    {
                        orderItem.ServedQty += item.ServeQty;
                        orderItem.RemainingQty = orderItem.Quantity - orderItem.ServedQty - orderItem.SoldOutQty;

                        if (orderItem.ServedQty >= orderItem.Quantity)
                            orderItem.KitchenStatus = "FullyServed";
                        else
                            orderItem.KitchenStatus = "PartiallyServed";

                        orderItem.UpdatedOnDate = DateTime.Now;
                        itemRepo.Update(orderItem);
                    }
                }

                await _unit.SaveAsync();

                // Recalculate status of affected KitchenRequests
                foreach (var reqId in affectedRequests)
                {
                    var req = await requestRepo.Get(x => x.Id == reqId).FirstOrDefaultAsync();
                    if (req != null)
                    {
                        var reqDetails = await detailRepo.Get(x => x.KitchenRequestId == reqId).ToListAsync();
                        if (reqDetails.All(x => x.RemainingQty == 0))
                        {
                            req.Status = "Completed";
                        }
                        else
                        {
                            req.Status = "InProgress";
                        }
                        requestRepo.Update(req);
                    }
                }

                await _unit.SaveAsync();
                await trans.CommitAsync();

                // Broadcast update event via SignalR to notify all kitchen screen clients
                foreach (var branchCode in affectedBranches)
                {
                    await _hubSender.SendRequestsUpdatedAsync(branchCode);
                }

                return new ResponseObject<ServeItemsResponseDto>
                {
                    Code = Code.Success,
                    Message = "Trả món thành công",
                    Data = new ServeItemsResponseDto
                    {
                        Success = true,
                        Message = "Trả món thành công",
                        PrintJobs = printJobs,
                        PrintSlips = serveSlipsByRequest.Values.ToList()
                    }
                };
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("KitchenHandler.ServeItems error", ex);
                return new Response(Code.ServerError, "Lỗi: " + ex.Message);
            }
        }

        public async Task<Response> SoldOutItems(SoldOutItemsRequestDto request)
        {
            try
            {
                if (request == null || request.Items == null || !request.Items.Any())
                    return new Response(Code.BadRequest, "Danh sách món báo hết không được để trống!");

                var detailRepo = _unit.GetRepository<KitchenRequestDetail>();
                var requestRepo = _unit.GetRepository<KitchenRequest>();
                var itemRepo = _unit.GetRepository<SalesOrderItem>();
                var menuItemRepo = _unit.GetRepository<MenuItem>();
                var preparationAreaRepo = _unit.GetRepository<PreparationArea>();
                var orderRepo = _unit.GetRepository<SalesOrder>();

                using var trans = await _unit.BeginTransactionAsync();
                var printJobs = new List<PrintJobDto>();
                var soldOutSlipsByRequest = new Dictionary<Guid, KitchenPrintSlipDto>();
                var affectedRequests = new HashSet<Guid>();
                var affectedBranches = new HashSet<string>();

                foreach (var item in request.Items)
                {
                    if (item.SoldOutQty <= 0)
                        return new Response(Code.BadRequest, "Số lượng báo hết phải lớn hơn 0!");

                    var detail = await detailRepo.Get(x => x.Id == item.RequestDetailId).FirstOrDefaultAsync();
                    if (detail == null)
                        return new Response(Code.NotFound, $"Không tìm thấy chi tiết yêu cầu với Id {item.RequestDetailId}!");

                    if (item.SoldOutQty > detail.RemainingQty)
                        return new Response(Code.BadRequest, $"Số lượng báo hết ({item.SoldOutQty}) vượt quá số lượng còn lại ({detail.RemainingQty}) của món '{detail.MenuItemName}'!");

                    var orderItem = await itemRepo.Get(x => x.Id == detail.OrderDetailId).FirstOrDefaultAsync();
                    var parentRequest = await requestRepo.Get(x => x.Id == detail.KitchenRequestId).FirstOrDefaultAsync();
                    if (parentRequest != null)
                    {
                        affectedRequests.Add(parentRequest.Id);
                        affectedBranches.Add(parentRequest.BranchCode);
                        if (!printJobs.Any(x => x.RequestCode == parentRequest.RequestCode))
                        {
                            printJobs.Add(new PrintJobDto
                            {
                                RequestCode = parentRequest.RequestCode,
                                PrintType = "SoldOutItem"
                            });
                        }

                        if (!soldOutSlipsByRequest.TryGetValue(parentRequest.Id, out var soldOutSlip))
                        {
                            var area = await preparationAreaRepo.Get(x => x.Id == parentRequest.KitchenAreaId).FirstOrDefaultAsync();
                            var order = await orderRepo.Get(x => x.Id == parentRequest.OrderId).FirstOrDefaultAsync();
                            soldOutSlip = new KitchenPrintSlipDto
                            {
                                SlipType = "SoldOut",
                                KitchenAreaId = parentRequest.KitchenAreaId,
                                KitchenAreaName = area?.Name ?? "Bếp / Bar",
                                DeviceType = area?.DeviceType ?? 2,
                                ProcessingArea = area?.ProcessingArea,
                                RequestCode = parentRequest.RequestCode + "-SOLDOUT",
                                OrderCode = parentRequest.OrderCode,
                                TableName = parentRequest.TableName,
                                BranchCode = parentRequest.BranchCode,
                                ServiceType = parentRequest.ServiceType,
                                WaiterName = order?.WaiterName,
                                CustomerCount = order?.CustomerCount ?? 0,
                                PrintTime = DateTime.Now,
                                OrderNote = order?.Note
                            };
                            soldOutSlipsByRequest[parentRequest.Id] = soldOutSlip;
                        }

                        soldOutSlip.Items.Add(new KitchenPrintItemDto
                        {
                            MenuItemId = detail.MenuItemId,
                            MenuItemName = detail.MenuItemName,
                            Quantity = item.SoldOutQty,
                            Note = orderItem != null ? KitchenNote(orderItem) : null,
                            Reason = "Báo hết món"
                        });
                    }

                    // Update detail quantities
                    detail.SoldOutQty += item.SoldOutQty;
                    detail.RemainingQty -= item.SoldOutQty;
                    if (detail.ServedQty + detail.SoldOutQty == detail.RequestQty)
                    {
                        detail.Status = "Completed";
                    }
                    else
                    {
                        detail.Status = "SoldOut";
                    }
                    detailRepo.Update(detail);

                    // Update corresponding SalesOrderItem
                    if (orderItem != null)
                    {
                        orderItem.SoldOutQty += item.SoldOutQty;
                        orderItem.RemainingQty = orderItem.Quantity - orderItem.ServedQty - orderItem.SoldOutQty;

                        if (orderItem.SoldOutQty >= orderItem.Quantity)
                            orderItem.KitchenStatus = "FullySoldOut";
                        else
                            orderItem.KitchenStatus = "PartiallySoldOut";

                        orderItem.UpdatedOnDate = DateTime.Now;
                        itemRepo.Update(orderItem);
                    }

                    // Only hide the menu item when the entire order line is sold out.
                    if (detail.MenuItemId != Guid.Empty && orderItem != null && orderItem.SoldOutQty >= orderItem.Quantity)
                    {
                        var menuItem = await menuItemRepo.Get(x => x.Id == detail.MenuItemId).FirstOrDefaultAsync();
                        if (menuItem != null && menuItem.IsAvailable)
                        {
                            menuItem.IsAvailable = false;
                            menuItem.DisabledAt = DateTime.Now;
                            menuItemRepo.Update(menuItem);
                        }
                    }
                }

                await _unit.SaveAsync();

                // Recalculate status of affected KitchenRequests
                foreach (var reqId in affectedRequests)
                {
                    var req = await requestRepo.Get(x => x.Id == reqId).FirstOrDefaultAsync();
                    if (req != null)
                    {
                        var reqDetails = await detailRepo.Get(x => x.KitchenRequestId == reqId).ToListAsync();
                        if (reqDetails.All(x => x.RemainingQty == 0))
                        {
                            req.Status = "Completed";
                        }
                        else
                        {
                            req.Status = "InProgress";
                        }
                        requestRepo.Update(req);
                    }
                }

                await _unit.SaveAsync();
                await trans.CommitAsync();

                // Broadcast update event via SignalR
                foreach (var branchCode in affectedBranches)
                {
                    await _hubSender.SendRequestsUpdatedAsync(branchCode);
                }

                return new ResponseObject<SoldOutItemsResponseDto>
                {
                    Code = Code.Success,
                    Message = "Báo hết món thành công",
                    Data = new SoldOutItemsResponseDto
                    {
                        Success = true,
                        Message = "Báo hết món thành công",
                        PrintJobs = printJobs,
                        PrintSlips = soldOutSlipsByRequest.Values.ToList()
                    }
                };
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("KitchenHandler.SoldOutItems error", ex);
                return new Response(Code.ServerError, "Lỗi: " + ex.Message);
            }
        }

        private async Task<KitchenRequestDto?> GetSingleRequestDto(Guid requestId)
        {
            var kitchenRequestRepo = _unit.GetRepository<KitchenRequest>();
            var kitchenRequestDetailRepo = _unit.GetRepository<KitchenRequestDetail>();
            var prepAreaRepo = _unit.GetRepository<PreparationArea>();
            var orderRepo = _unit.GetRepository<SalesOrder>();

            var req = await kitchenRequestRepo.Get(x => x.Id == requestId).FirstOrDefaultAsync();
            if (req == null) return null;

            var order = await orderRepo.Get(x => x.Id == req.OrderId).FirstOrDefaultAsync();
            var floorName = order?.FloorName;

            var area = await prepAreaRepo.Get(x => x.Id == req.KitchenAreaId).FirstOrDefaultAsync();
            var areaName = area?.Name ?? "Bếp";

            var orderItemRepo = _unit.GetRepository<SalesOrderItem>();
            var detailsWithNote = await (from d in kitchenRequestDetailRepo.Get(x => x.KitchenRequestId == requestId)
                                         join oi in orderItemRepo.Get(x => true) on d.OrderDetailId equals oi.Id into ois
                                         from oi in ois.DefaultIfEmpty()
                                         select new {
                                             Detail = d,
                                             OrderItem = oi
                                         }).ToListAsync();

            return new KitchenRequestDto
            {
                RequestId = req.Id,
                RequestCode = req.RequestCode,
                OrderCode = req.OrderCode,
                ServiceType = req.ServiceType,
                TableName = req.TableName ?? "Bàn",
                FloorName = floorName,
                RequestedAt = req.RequestedAt,
                ElapsedSeconds = 0,
                KitchenAreaName = areaName,
                Details = detailsWithNote.Select(d => new KitchenRequestDetailDto
                {
                    RequestDetailId = d.Detail.Id,
                    MenuItemId = d.Detail.MenuItemId,
                    MenuItemName = d.Detail.MenuItemName,
                    RequestQty = d.Detail.RequestQty,
                    ServedQty = d.Detail.ServedQty,
                    SoldOutQty = d.Detail.SoldOutQty,
                    RemainingQty = d.Detail.RemainingQty,
                    Note = d.OrderItem != null ? KitchenNote(d.OrderItem) : null
                }).ToList()
            };
        }

        private async Task<int> GetNextKitchenRequestSequence(string branchCode, string orderCode)
        {
            var requestRepo = _unit.GetRepository<KitchenRequest>();
            var prefix = $"{orderCode}-";

            var allCodes = await requestRepo.Get(x => x.BranchCode == branchCode && x.RequestCode.StartsWith(prefix))
                                            .Select(x => x.RequestCode)
                                            .ToListAsync();

            int maxSeq = 0;
            foreach (var code in allCodes)
            {
                if (!string.IsNullOrEmpty(code) && code.Length >= prefix.Length)
                {
                    var seqPart = code.Substring(prefix.Length);
                    if (int.TryParse(seqPart, out int parsedNum))
                    {
                        if (parsedNum > maxSeq)
                        {
                            maxSeq = parsedNum;
                        }
                    }
                }
            }

            return maxSeq + 1;
        }
    }
}
