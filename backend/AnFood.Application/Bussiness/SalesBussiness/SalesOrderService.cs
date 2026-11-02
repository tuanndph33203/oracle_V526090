using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnFood.Application.Bussiness.AppLogger;
using AnFood.Domain.Datatables;
using AnFood.Domain.Enums;
using AnFood.Infrastructure;
using AnFood.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using AnFood.Application.Services.Mqtt;
using AnFood.Application.Bussiness.MenuItemBussiness;

namespace AnFood.Application.Bussiness.SalesBussiness
{
    /// <summary>
    /// Nền tảng dữ liệu Order/Bill — phục vụ tất cả các luồng:
    /// Mở bàn, Đặt trước, Mang về, Giao hàng, Gửi bếp/bar,
    /// Chuyển bàn, Gộp bàn, Tách bàn, Sửa order, Kiểm đồ,
    /// Thanh toán, Yêu cầu chung.
    ///
    /// RULE: Mọi thao tác đều validate BranchCode trước.
    ///       Tuyệt đối không đọc/ghi dữ liệu khác chi nhánh.
    /// </summary>
    public class SalesOrderService : ISalesOrderService
    {
        private readonly IUnitOfWork _unit;
        private readonly AnFood.Application.Bussiness.MenuVisibility.MenuVisibilityService _visibility;
        private readonly IAppLogger  _logger;
        private readonly AnFood.Application.Bussiness.KitchenBussiness.IKitchenHandler _kitchenHandler;
        private readonly AnFood.Application.Bussiness.KitchenBussiness.IKitchenHubSender _kitchenHubSender;
        private readonly IMqttSpeakerService _mqttSpeakerService;
        private readonly IConfiguration? _config;

        public SalesOrderService(
            IUnitOfWork unit, 
            IAppLogger logger, 
            AnFood.Application.Bussiness.KitchenBussiness.IKitchenHandler kitchenHandler,
            AnFood.Application.Bussiness.KitchenBussiness.IKitchenHubSender kitchenHubSender,
            IMqttSpeakerService mqttSpeakerService,
            AnFood.Application.Bussiness.MenuVisibility.MenuVisibilityService visibility,
            IConfiguration? config = null)
        {
            _unit   = unit;
            _visibility = visibility;
            _logger = logger;
            _kitchenHandler = kitchenHandler;
            _kitchenHubSender = kitchenHubSender;
            _mqttSpeakerService = mqttSpeakerService;
            _config = config;
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

        private async Task<string?> PrepareMenuOptionsAsync(CreateOrUpdateOrderRequest request)
        {
            var newItems = (request.Items ?? new()).Where(x => !x.Id.HasValue || x.Id == Guid.Empty).ToList();
            if (newItems.Count == 0) return null;
            var ids = newItems.Select(x => x.ItemId).Distinct().ToList();
            var menus = await _unit.GetRepository<MenuItem>().Get(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
            var taxRateIds = menus.Values.Where(x => x.TaxRateId.HasValue).Select(x => x.TaxRateId!.Value).Distinct().ToList();
            var taxRates = await _unit.GetRepository<TaxRate>().Get(x => taxRateIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Rate);
            foreach (var item in newItems)
            {
                if (!menus.TryGetValue(item.ItemId, out var menu)) return "Món ăn không tồn tại.";
                var groups = string.IsNullOrWhiteSpace(menu.OptionGroupsJson)
                    ? new List<MenuOptionGroupModel>()
                    : System.Text.Json.JsonSerializer.Deserialize<List<MenuOptionGroupModel>>(menu.OptionGroupsJson) ?? new();
                if (groups.Count == 0)
                {
                    if (item.SelectedOptions?.Count > 0) return "Món ăn không có tùy chọn.";
                    continue;
                }

                var selected = item.SelectedOptions ?? new();
                var snapshots = new List<OrderSelectedOptionDto>();
                foreach (var group in groups)
                {
                    var chosen = selected.Where(x => string.Equals(x.GroupCode, group.Code, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (group.Required && chosen.Count == 0) return $"Vui lòng chọn {group.Name}.";
                    if (!group.Multiple && chosen.Count > 1) return $"Chỉ được chọn một giá trị cho {group.Name}.";
                    if (chosen.Any(x => string.IsNullOrWhiteSpace(x.ValueCode)) ||
                        chosen.Select(x => x.ValueCode.ToUpperInvariant()).Distinct().Count() != chosen.Count)
                        return $"Giá trị {group.Name} bị chọn trùng.";
                    foreach (var choice in chosen)
                    {
                        var value = group.Values.FirstOrDefault(x => string.Equals(x.Code, choice.ValueCode, StringComparison.OrdinalIgnoreCase));
                        if (value == null) return $"Giá trị tùy chọn không hợp lệ trong {group.Name}.";
                        snapshots.Add(new OrderSelectedOptionDto
                        {
                            GroupCode = group.Code, GroupName = group.Name,
                            ValueCode = value.Code, ValueName = value.Name, ExtraCharge = value.ExtraCharge,
                            FixedTax = value.FixedTax
                        });
                    }
                }
                if (snapshots.Count != selected.Count) return "Nhóm tùy chọn không hợp lệ.";
                item.UnitPrice = menu.SalePrice + snapshots.Sum(x => x.ExtraCharge);
                if (item.UnitPrice < 0) return $"Tùy chọn làm đơn giá món {menu.Name} âm.";
                item.SelectedOptionsJson = System.Text.Json.JsonSerializer.Serialize(snapshots);
                var vatRate = menu.TaxRateId.HasValue && taxRates.TryGetValue(menu.TaxRateId.Value, out var rate) ? (double)rate : 0d;
                if (item.UnitPrice + CalculateUnitVat(item.UnitPrice, vatRate, menu, item.SelectedOptionsJson) < 0)
                    return $"Tùy chọn làm tổng giá món {menu.Name} âm.";
            }
            return null;
        }

        private Task BroadcastSalesEventAsync(string branchCode, SalesRealtimeEventDto eventData)
        {
            return Task.CompletedTask;
        }

        private async Task<bool> IsKitchenPrintEnabled()
        {
            var setting = await _unit.GetRepository<SystemSetting>()
                .Get(x => x.Code.ToLower() == Constant.SystemSettingCodes.SaleEnableKitchenPrint.ToLower())
                .FirstOrDefaultAsync();
            return setting != null && setting.IsActive && Constant.SystemSettingAllowsAction(setting.Value);
        }

        // ═══════════════════════════════════════════════════════
        // GET ORDER BY ID
        // ═══════════════════════════════════════════════════════

        public async Task<Response> GetOrderById(Guid orderId, string branchCode)
        {
            try
            {
                // ── Validate inputs ──────────────────────────────
                if (string.IsNullOrWhiteSpace(branchCode))
                    return new Response(Code.BadRequest, "Mã chi nhánh không được để trống!");

                if (orderId == Guid.Empty)
                    return new Response(Code.BadRequest, "OrderId không hợp lệ!");

                // ── Lấy order ────────────────────────────────────
                var orderRepo = _unit.GetRepository<SalesOrder>();
                var order = await orderRepo
                    .Get(x => x.Id == orderId && x.BranchCode == branchCode)
                    .FirstOrDefaultAsync();

                if (order == null)
                    return new Response(Code.NotFound, "Order không tồn tại hoặc không thuộc chi nhánh này!");

                // ── Lấy items ────────────────────────────────────
                var itemRepo = _unit.GetRepository<SalesOrderItem>();
                var items = await itemRepo
                    .Get(x => x.OrderId == orderId && x.BranchCode == branchCode)
                    .OrderBy(x => x.LineNo)
                    .ToListAsync();

                // ── Lấy tables ───────────────────────────────────
                var tableRepo = _unit.GetRepository<SalesOrderTable>();
                var tables = await tableRepo
                    .Get(x => x.OrderId == orderId && x.BranchCode == branchCode)
                    .ToListAsync();

                // ── Lấy payments ─────────────────────────────────
                var paymentRepo = _unit.GetRepository<SalesOrderPayment>();
                var payments = await paymentRepo
                    .Get(x => x.OrderId == orderId && x.BranchCode == branchCode)
                    .OrderBy(x => x.CreatedOnDate)
                    .ToListAsync();

                // ── Map → DTO ────────────────────────────────────
                var dto = await MapOrderToDtoAsync(order, items, tables, payments);

                return new ResponseObject<SalesOrderDto>
                {
                    Code    = Code.Success,
                    Message = "Lấy thông tin order thành công!",
                    Data    = dto
                };
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("SalesOrderService.GetOrderById", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi hệ thống: " + ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════
        // GET ACTIVE ORDERS BY TABLE
        // ═══════════════════════════════════════════════════════

        public async Task<Response> GetActiveOrdersByTable(Guid tableId, string branchCode)
        {
            try
            {
                // ── Validate inputs ──────────────────────────────
                if (string.IsNullOrWhiteSpace(branchCode))
                    return new Response(Code.BadRequest, "Mã chi nhánh không được để trống!");

                if (tableId == Guid.Empty)
                    return new Response(Code.BadRequest, "TableId không hợp lệ!");

                // ── Validate table thuộc đúng branch ─────────────
                if (!await ValidateTableBranch(tableId, branchCode))
                    return new Response(Code.BadRequest, "Bàn không tồn tại hoặc không thuộc chi nhánh này!");

                // ── Lấy bàn để lấy tên ───────────────────────────
                var diningTableRepo = _unit.GetRepository<DiningTable>();
                var diningTable = await diningTableRepo
                    .Get(x => x.Id == tableId && x.Status != 0)
                    .FirstOrDefaultAsync();

                // ── Lấy order table links ─────────────────────────
                var orderTableRepo = _unit.GetRepository<SalesOrderTable>();
                var linkedOrderIds = await orderTableRepo
                    .Get(x => x.TableId == tableId && x.BranchCode == branchCode)
                    .Select(x => x.OrderId)
                    .ToListAsync();

                if (!linkedOrderIds.Any())
                {
                    return new ResponseObject<TableActiveOrdersDto>
                    {
                        Code    = Code.Success,
                        Message = "Bàn chưa có order nào đang hoạt động.",
                        Data    = new TableActiveOrdersDto
                        {
                            TableId    = tableId,
                            TableCode  = diningTable?.Code ?? string.Empty,
                            TableName  = diningTable?.Name ?? string.Empty,
                            BranchCode = branchCode,
                            Orders     = new List<ActiveOrderDto>()
                        }
                    };
                }

                // ── Lọc chỉ lấy orders đang active ───────────────
                var orderRepo = _unit.GetRepository<SalesOrder>();
                var activeOrders = await orderRepo
                    .Get(x => linkedOrderIds.Contains(x.Id)
                            && x.BranchCode == branchCode
                            && !OrderStatusConstants.InactiveStatuses.Contains(x.Status))
                    .OrderBy(x => x.OpenTime)
                    .ToListAsync();

                // ── Lấy item counts ───────────────────────────────
                var activeOrderIds = activeOrders.Select(o => o.Id).ToList();
                var itemRepo = _unit.GetRepository<SalesOrderItem>();
                var allItems = await itemRepo
                    .Get(x => activeOrderIds.Contains(x.OrderId) && x.BranchCode == branchCode)
                    .ToListAsync();

                // ── Map → ActiveOrderDto ──────────────────────────
                var activeDtos = activeOrders.Select(o =>
                {
                    var oItems = allItems.Where(i => i.OrderId == o.Id).ToList();
                    return new ActiveOrderDto
                    {
                        OrderId       = o.Id,
                        OrderCode     = o.OrderCode,
                        Status        = o.Status,
                        ServiceType   = o.ServiceType,
                        OpenTime      = o.OpenTime,
                        CustomerCount = o.CustomerCount,
                        WaiterId      = o.WaiterId,
                        WaiterName    = o.WaiterName,
                        TotalAmount   = o.TotalAmount,
                        ItemCount     = oItems.Count(i => i.IsCancelled == 0),
                        UnsentItems   = oItems.Count(i => i.IsSentToKitchen == 0 && i.IsCancelled == 0)
                    };
                }).ToList();

                var activeCustomerCount = activeDtos.Sum(o => o.CustomerCount);
                var capacity = diningTable?.Capacity ?? 0;

                var result = new TableActiveOrdersDto
                {
                    TableId             = tableId,
                    TableCode           = diningTable?.Code ?? string.Empty,
                    TableName           = diningTable?.Name ?? string.Empty,
                    BranchCode          = branchCode,
                    Capacity            = capacity,
                    ActiveOrderCount    = activeDtos.Count,
                    ActiveCustomerCount = activeCustomerCount,
                    RemainingCapacity   = Math.Max(0, capacity - activeCustomerCount),
                    Orders              = activeDtos
                };

                return new ResponseObject<TableActiveOrdersDto>
                {
                    Code    = Code.Success,
                    Message = $"Bàn đang có {activeDtos.Count} order active.",
                    Data    = result
                };
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("SalesOrderService.GetActiveOrdersByTable", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi hệ thống: " + ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════
        // GET ORDERS BY SERVICE TYPE
        // ═══════════════════════════════════════════════════════

        public async Task<Response> GetOrdersByServiceType(string branchCode, string serviceType, string? statuses)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(branchCode))
                    return new Response(Code.BadRequest, "Mã chi nhánh không được để trống!");

                bool isAllServiceTypes = string.IsNullOrWhiteSpace(serviceType) || serviceType.Equals("All", StringComparison.OrdinalIgnoreCase);

                // Parse statuses
                var statusList = new List<string>();
                if (!string.IsNullOrWhiteSpace(statuses))
                {
                    statusList = statuses.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToList();
                }
                else
                {
                    statusList = new List<string> { OrderStatusConstants.WaitingService, OrderStatusConstants.Serving, OrderStatusConstants.WaitingPayment, OrderStatusConstants.Reserved, OrderStatusConstants.Checked };
                }

                // Enforce active statuses (WaitingService, Serving, WaitingPayment, Reserved, Checked, WaitingConfirm)
                statusList = statusList.Where(s => s == OrderStatusConstants.WaitingService || s == OrderStatusConstants.Serving || s == OrderStatusConstants.WaitingPayment || s == OrderStatusConstants.Reserved || s == OrderStatusConstants.Checked || s == OrderStatusConstants.WaitingConfirm).ToList();
                if (!statusList.Any())
                {
                    statusList = new List<string> { OrderStatusConstants.WaitingService, OrderStatusConstants.Serving, OrderStatusConstants.WaitingPayment, OrderStatusConstants.Reserved, OrderStatusConstants.Checked, OrderStatusConstants.WaitingConfirm };
                }

                var orderRepo = _unit.GetRepository<SalesOrder>();
                var orders = await orderRepo
                    .Get(x => x.BranchCode == branchCode
                           && (isAllServiceTypes || x.ServiceType == serviceType)
                           && statusList.Contains(x.Status))
                    .OrderByDescending(x => x.OpenTime)
                    .ToListAsync();

                var orderIds = orders.Select(o => o.Id).ToList();
                var itemRepo = _unit.GetRepository<SalesOrderItem>();
                var allItems = await itemRepo
                    .Get(x => orderIds.Contains(x.OrderId) && x.BranchCode == branchCode && x.IsCancelled == 0)
                    .ToListAsync();

                var orderTableRepo = _unit.GetRepository<SalesOrderTable>();
                var allOrderTables = await orderTableRepo
                    .Get(x => orderIds.Contains(x.OrderId))
                    .ToListAsync();

                var tableIds = allOrderTables.Select(t => t.TableId).Distinct().ToList();

                var allActiveOrderTables = await orderTableRepo
                    .Get(x => x.BranchCode == branchCode && tableIds.Contains(x.TableId))
                    .ToListAsync();
                
                var activeOrderIdsForTables = allActiveOrderTables.Select(x => x.OrderId).Distinct().ToList();

                var activeOrdersForTables = await orderRepo
                    .Get(x => x.BranchCode == branchCode 
                           && activeOrderIdsForTables.Contains(x.Id)
                           && x.Status != OrderStatusConstants.Paid 
                           && x.Status != OrderStatusConstants.Completed 
                           && x.Status != OrderStatusConstants.Cancelled 
                           && x.Status != OrderStatusConstants.ApprovedCancelled
                           && x.Status != OrderStatusConstants.Merged
                           && x.Status != OrderStatusConstants.WaitingConfirm)
                    .ToListAsync();

                var phoneNumbers = orders
                    .Where(o => !string.IsNullOrWhiteSpace(o.PhoneNumber))
                    .Select(o => o.PhoneNumber!.Trim())
                    .Distinct()
                    .ToList();

                var rawPhoneNumbers = orders
                    .Where(o => !string.IsNullOrWhiteSpace(o.PhoneNumber))
                    .Select(o => o.PhoneNumber!)
                    .Distinct()
                    .ToList();

                var phoneCandidates = phoneNumbers.Concat(rawPhoneNumbers).Distinct().ToList();

                var activeOrdersForPhones = phoneCandidates.Any()
                    ? await orderRepo
                        .Get(x => x.BranchCode == branchCode 
                               && x.PhoneNumber != null
                               && phoneCandidates.Contains(x.PhoneNumber)
                               && x.Status != OrderStatusConstants.Paid 
                               && x.Status != OrderStatusConstants.Completed 
                               && x.Status != OrderStatusConstants.Cancelled 
                               && x.Status != OrderStatusConstants.ApprovedCancelled
                               && x.Status != OrderStatusConstants.Merged)
                        .OrderByDescending(x => x.OpenTime)
                        .ToListAsync()
                    : new List<SalesOrder>();

                var orderDtos = orders.Select(o =>
                {
                    var oItems = allItems.Where(i => i.OrderId == o.Id).ToList();
                    var hasUnsent = oItems.Any(i => i.IsSentToKitchen == 0 && i.IsCancelled == 0);
                    var oTables = allOrderTables.Where(t => t.OrderId == o.Id).ToList();
                    var tableName = oTables.Any() ? string.Join(", ", oTables.Select(t => t.TableName)) :
                        o.ServiceType == ServiceTypeConstants.TakeAway ? "Mang về" :
                        o.ServiceType == ServiceTypeConstants.Delivery ? "Giao hàng" : null;
                    var tableId = oTables.Any() ? (Guid?)oTables.First().TableId : null;

                    var activeBills = new List<ActiveOrderMapInfo>();
                    if (tableId.HasValue)
                    {
                        var linkedActiveOrderIds = allActiveOrderTables
                            .Where(x => x.TableId == tableId.Value)
                            .Select(x => x.OrderId)
                            .ToList();

                        activeBills = activeOrdersForTables
                            .Where(x => linkedActiveOrderIds.Contains(x.Id))
                            .Select(x => new ActiveOrderMapInfo
                            {
                                OrderId = x.Id,
                                OrderCode = x.OrderCode,
                                Status = x.Status,
                                CustomerCount = x.CustomerCount,
                                OpenTime = x.OpenTime,
                                TableName = tableName,
                                ServiceType = x.ServiceType
                            })
                            .ToList();
                    }
                    else if (!string.IsNullOrWhiteSpace(o.PhoneNumber))
                    {
                        var normPhone = o.PhoneNumber.Trim();
                        activeBills = activeOrdersForPhones
                            .Where(x => x.Id != o.Id
                                   && !string.IsNullOrWhiteSpace(x.PhoneNumber)
                                   && x.PhoneNumber.Trim() == normPhone
                                   && x.ServiceType == o.ServiceType
                                   && x.OrderDate == o.OrderDate)
                            .Select(x => new ActiveOrderMapInfo
                            {
                                OrderId = x.Id,
                                OrderCode = x.OrderCode,
                                Status = x.Status,
                                CustomerCount = x.CustomerCount,
                                OpenTime = x.OpenTime,
                                ServiceType = x.ServiceType,
                                TableName = x.ServiceType == ServiceTypeConstants.TakeAway ? "Mang về" :
                                            x.ServiceType == ServiceTypeConstants.Delivery ? "Giao hàng" : null
                            })
                            .ToList();
                    }
                    
                    decimal totalAmount = o.TotalAmount > 0
                        ? o.TotalAmount
                        : (oItems.Any() ? oItems.Sum(x => x.TotalAmount > 0 ? x.TotalAmount : (decimal)x.Quantity * x.UnitPrice) : 0);

                    return new ServiceTypeOrderDto
                    {
                        OrderId = o.Id,
                        OrderCode = o.OrderCode,
                        ServiceType = o.ServiceType,
                        Status = o.Status,
                        CustomerId = o.CustomerId,
                        CustomerName = o.CustomerName,
                        PhoneNumber = o.PhoneNumber,
                        DeliveryAddress = o.DeliveryAddress,
                        DeliveryNote = o.DeliveryNote,
                        Note = o.Note,
                        OpenTime = o.OpenTime,
                        TotalAmount = totalAmount,
                        CashierUserName = o.CashierUserName,
                        WaiterName = o.WaiterName,
                        HasUnsentKitchenItems = hasUnsent,
                        TableName = tableName,
                        FloorId = o.FloorId,
                        FloorName = o.FloorName,
                        TableId = tableId,
                        ActiveOrders = activeBills
                    };
                }).ToList();

                var result = new OrdersByServiceTypeDto
                {
                    BranchCode = branchCode,
                    ServiceType = serviceType,
                    Orders = orderDtos
                };

                return new ResponseObject<OrdersByServiceTypeDto>
                {
                    Code = Code.Success,
                    Message = "Lấy danh sách đơn hàng thành công!",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("SalesOrderService.GetOrdersByServiceType", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi: " + ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════
        // VALIDATE TABLE CAPACITY
        // ═══════════════════════════════════════════════════════

        public async Task<Response> ValidateTableCapacityForOrder(string branchCode, List<Guid> tableIds, int customerCount, List<Guid>? excludeOrderIds = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(branchCode))
                    return new Response(Code.BadRequest, "Mã chi nhánh không được để trống!");

                if (tableIds == null || !tableIds.Any())
                    return new Response(Code.Success, "Không có bàn để validate.");

                if (customerCount < 1)
                    return new Response(Code.BadRequest, "Số lượng khách phải lớn hơn hoặc bằng 1!");

                var branchRepo = _unit.GetRepository<Branchs>();
                var branch = await branchRepo.Get(x => x.Code == branchCode && x.Status != 0).FirstOrDefaultAsync();
                if (branch == null)
                    return new Response(Code.NotFound, "Chi nhánh không tồn tại!");

                var tableRepo = _unit.GetRepository<DiningTable>();
                var tables = await tableRepo.Get(x => tableIds.Contains(x.Id) && x.BranchId == branch.Id && x.Status != 0).ToListAsync();

                if (tables.Count != tableIds.Count)
                {
                    return new Response(Code.BadRequest, "Có bàn không tồn tại hoặc không thuộc chi nhánh này!");
                }

                return new Response(Code.Success, "Validate sức chứa thành công!");
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("SalesOrderService.ValidateTableCapacityForOrder", ex);
                return new Response(Code.ServerError, "Lỗi kiểm tra sức chứa bàn: " + ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════
        // SAVE ORDER (CREATE / UPDATE)
        // ═══════════════════════════════════════════════════════

        public async Task<Response> SaveOrder(CreateOrUpdateOrderRequest request, string? userCode = null, string? userName = null)
        {
            try
            {
                // ── Validate BranchCode ──────────────────────────
                if (string.IsNullOrWhiteSpace(request.BranchCode))
                    return new Response(Code.BadRequest, "Mã chi nhánh không được để trống!");

                // ── Validate Branch tồn tại ──────────────────────
                var branchRepo = _unit.GetRepository<Branchs>();
                var branch = await branchRepo
                    .Get(x => x.Code == request.BranchCode && x.Status != 0)
                    .FirstOrDefaultAsync();
                if (branch == null)
                    return new Response(Code.NotFound, "Chi nhánh không tồn tại hoặc đã bị khóa!");

                var optionError = await PrepareMenuOptionsAsync(request);
                if (optionError != null) return new Response(Code.BadRequest, optionError);

                // ── Validate CustomerCount ───────────────────────
                if (request.CustomerCount < 1)
                    return new Response(Code.BadRequest, "Số lượng khách phải lớn hơn hoặc bằng 1!");

                // Check if the order already exists in the database
                var orderRepo = _unit.GetRepository<SalesOrder>();
                bool orderExists = false;
                if (request.OrderId.HasValue && request.OrderId.Value != Guid.Empty)
                {
                    orderExists = await orderRepo.Get(x => x.Id == request.OrderId.Value && x.BranchCode == request.BranchCode).AnyAsync();
                }

                if (!orderExists)
                {
                    // ── Validate Items for new orders ─────────────────
                    if (request.Items == null || request.Items.Count == 0)
                        return new Response(Code.BadRequest, "Order phải có ít nhất một món!");

                    foreach (var item in request.Items)
                    {
                        if (item.Quantity <= 0)
                            return new Response(Code.BadRequest, $"Số lượng món '{item.ItemName}' phải lớn hơn 0!");
                        if (item.UnitPrice < 0)
                            return new Response(Code.BadRequest, $"Đơn giá món '{item.ItemName}' không được âm!");
                    }
                }
                else
                {
                    // ── Validate Items for existing orders ────────────
                    if (request.Items != null)
                    {
                        foreach (var item in request.Items)
                        {
                            if (item.Quantity < 0)
                                return new Response(Code.BadRequest, $"Số lượng món '{item.ItemName}' không được âm!");
                            if (item.UnitPrice < 0)
                                return new Response(Code.BadRequest, $"Đơn giá món '{item.ItemName}' không được âm!");
                        }
                    }
                }

                // ── Validate ServiceType and associated fields ───
                string serviceType = string.IsNullOrWhiteSpace(request.ServiceType) ? ServiceTypeConstants.DineIn : request.ServiceType;
                if (serviceType == ServiceTypeConstants.DineIn)
                {
                    request.TableIds = request.TableIds ?? new List<Guid>();
                }
                else if (serviceType == ServiceTypeConstants.TakeAway)
                {
                    request.TableIds = new List<Guid>();
                }
                else if (serviceType == ServiceTypeConstants.Delivery)
                {
                    request.TableIds = new List<Guid>();
                    if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                        return new Response(Code.BadRequest, "Số điện thoại là bắt buộc đối với đơn giao hàng!");
                    if (!System.Text.RegularExpressions.Regex.IsMatch(request.PhoneNumber, @"^\+?[0-9\s\-\.]{7,15}$"))
                        return new Response(Code.BadRequest, "Số điện thoại không đúng định dạng!");
                    if (string.IsNullOrWhiteSpace(request.DeliveryAddress))
                        return new Response(Code.BadRequest, "Địa chỉ giao hàng là bắt buộc đối với đơn giao hàng!");
                }

                // Default customer properties for TakeAway/Delivery
                if (serviceType == ServiceTypeConstants.TakeAway || serviceType == ServiceTypeConstants.Delivery)
                {
                    if (string.IsNullOrWhiteSpace(request.CustomerName))
                    {
                        request.CustomerName = "Khách lẻ";
                    }
                    if (string.IsNullOrWhiteSpace(request.CustomerCode))
                    {
                        request.CustomerCode = "GUEST";
                    }
                }

                // ── Validate Tables thuộc đúng branch ───────────
                foreach (var tableId in request.TableIds)
                {
                    if (!await ValidateTableBranch(tableId, request.BranchCode))
                        return new Response(Code.BadRequest, $"Bàn '{tableId}' không tồn tại hoặc không thuộc chi nhánh này!");
                }

                // ── Validate Capacity ────────────────────────────
                if (request.TableIds != null && request.TableIds.Any())
                {
                    var excludeList = request.OrderId.HasValue && request.OrderId.Value != Guid.Empty
                        ? new List<Guid> { request.OrderId.Value }
                        : null;
                    var capRes = await ValidateTableCapacityForOrder(request.BranchCode, request.TableIds, request.CustomerCount, excludeList);
                    if (capRes.Code != Code.Success)
                    {
                        return capRes;
                    }
                }

                var itemRepo      = _unit.GetRepository<SalesOrderItem>();
                var orderTableRepo = _unit.GetRepository<SalesOrderTable>();

                bool isCreate = request.OrderId == null || request.OrderId == Guid.Empty;

                if (isCreate)
                {
                    return await CreateNewOrder(request, branch, orderRepo, itemRepo, orderTableRepo, userCode, userName);
                }
                else
                {
                    // ── Validate order thuộc đúng branch ────────
                    if (!await ValidateOrderBranch(request.OrderId!.Value, request.BranchCode))
                        return new Response(Code.BadRequest, "Order không tồn tại hoặc không thuộc chi nhánh này!");

                    return await UpdateExistingOrder(request, orderRepo, itemRepo, orderTableRepo, userCode, userName);
                }
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("SalesOrderService.SaveOrder", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi hệ thống: " + ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════
        // CANCEL ORDER
        // ═══════════════════════════════════════════════════════

        public async Task<Response> CancelOrder(Guid orderId, string branchCode, string reason, string? userCode = null, string? userName = null)
        {
            try
            {
                // ── Validate inputs ──────────────────────────────
                if (string.IsNullOrWhiteSpace(branchCode))
                    return new Response(Code.BadRequest, "Mã chi nhánh không được để trống!");

                if (orderId == Guid.Empty)
                    return new Response(Code.BadRequest, "OrderId không hợp lệ!");

                if (string.IsNullOrWhiteSpace(reason))
                    return new Response(Code.BadRequest, "Lý do hủy không được để trống!");

                // ── Validate order thuộc đúng branch ────────────
                var orderRepo = _unit.GetRepository<SalesOrder>();
                var order = await orderRepo
                    .Get(x => x.Id == orderId && x.BranchCode == branchCode)
                    .FirstOrDefaultAsync();

                if (order == null)
                    return new Response(Code.NotFound, "Order không tồn tại hoặc không thuộc chi nhánh này!");

                // ── Kiểm tra trạng thái ──────────────────────────
                if (order.Status == OrderStatusConstants.Cancelled || order.Status == OrderStatusConstants.ApprovedCancelled)
                    return new Response(Code.BadRequest, "Order đã được hủy trước đó!");

                if (order.Status == OrderStatusConstants.Paid || order.Status == OrderStatusConstants.Completed)
                    return new Response(Code.BadRequest, "Không thể hủy order đã thanh toán!");

                var oldStatus = order.Status;
                if (!OrderStatusConstants.CanTransition(oldStatus, OrderStatusConstants.Cancelled, "Cancel"))
                {
                    return new Response(Code.BadRequest, $"Không thể hủy order ở trạng thái '{oldStatus}'!");
                }

                // ── Cập nhật trạng thái ──────────────────────────
                order.Status        = OrderStatusConstants.Cancelled;
                order.CancelReason  = reason;
                order.UpdatedOnDate = DateTime.Now;

                using var trans = await _unit.BeginTransactionAsync();

                // ── Cập nhật trạng thái ──────────────────────────
                order.Status        = OrderStatusConstants.Cancelled;
                order.CancelReason  = reason;
                order.UpdatedOnDate = DateTime.Now;
                if (!string.IsNullOrEmpty(userCode)) order.UpdatedByUserCode = userCode;
                if (!string.IsNullOrEmpty(userName)) order.UpdatedByUserName = userName;

                orderRepo.Update(order);

                // ── Cancel items and kitchen requests (BE-03) ────
                var itemRepo = _unit.GetRepository<SalesOrderItem>();
                var items = await itemRepo
                    .Get(x => x.OrderId == orderId && x.BranchCode == branchCode)
                    .ToListAsync();
                var hadKitchenSentItems = items.Any(x => x.IsCancelled == 0 && x.SentQty > 0);

                var now = DateTime.Now;
                var affectedRequestIds = new HashSet<Guid>();

                var detailRepo = _unit.GetRepository<KitchenRequestDetail>();
                var requestRepo = _unit.GetRepository<KitchenRequest>();

                var cancelMenuItemRepo = _unit.GetRepository<MenuItem>();
                var cancelItemIds = items.Where(x => x.IsCancelled == 0).Select(x => x.ItemId).Distinct().ToList();
                var cancelMenuItems = await cancelMenuItemRepo.Get(x => cancelItemIds.Contains(x.Id)).ToListAsync();
                var cancelMenuItemDict = cancelMenuItems.ToDictionary(x => x.Id, x => x);

                foreach (var item in items)
                {
                    if (item.IsCancelled == 0)
                    {
                        item.IsCancelled = 1;
                        item.KitchenStatus = "Cancelled";
                        item.RemainingQty = 0;
                        item.UpdatedOnDate = now;
                        itemRepo.Update(item);

                        // Hoàn lại số lượng tồn kho nếu món có quản lý tồn
                        if (cancelMenuItemDict.TryGetValue(item.ItemId, out var cmi) && cmi.IsTrackStock)
                        {
                            cmi.StockQuantity = (cmi.StockQuantity ?? 0) + (int)item.Quantity;
                            if (cmi.StockQuantity > 0)
                            {
                                cmi.IsAvailable = true;
                                cmi.DisabledAt = null;
                            }
                            cancelMenuItemRepo.Update(cmi);
                        }

                        // Cancel pending kitchen requests for this item
                        var pendingDetails = await detailRepo
                            .Get(x => x.OrderDetailId == item.Id && x.RemainingQty > 0)
                            .ToListAsync();
                        foreach (var pd in pendingDetails)
                        {
                            pd.RemainingQty = 0;
                            pd.Status = "Completed";
                            detailRepo.Update(pd);
                            affectedRequestIds.Add(pd.KitchenRequestId);
                        }
                    }
                }

                // Recalculate status of affected KitchenRequests
                foreach (var reqId in affectedRequestIds)
                {
                    var req = await requestRepo.Get(x => x.Id == reqId).FirstOrDefaultAsync();
                    if (req != null)
                    {
                        var reqDetails = await detailRepo.Get(x => x.KitchenRequestId == reqId).ToListAsync();
                        if (reqDetails.All(x => x.RemainingQty == 0))
                        {
                            req.Status = "Completed";
                            requestRepo.Update(req);
                        }
                    }
                }

                await _unit.SaveAsync();
                await trans.CommitAsync();

                try
                {
                    await _kitchenHubSender.SendRequestsUpdatedAsync(branchCode);
                }
                catch (Exception hubEx)
                {
                    await _logger.ErrorAsync($"Error pushing SignalR update after order cancellation: {hubEx.Message}", hubEx);
                }

                await _logger.InfoAsync($"Order status changed: OrderId={orderId}, BranchCode={branchCode}, Action=Cancel, OldStatus={oldStatus}, NewStatus={OrderStatusConstants.Cancelled}");

                await BroadcastSalesEventAsync(branchCode, new SalesRealtimeEventDto
                {
                    EventType = "OrderCancelled",
                    OrderId = order.Id,
                    OrderCode = order.OrderCode,
                    NewStatus = OrderStatusConstants.Cancelled,
                    ServiceType = order.ServiceType
                });

                return new ResponseObject<OrderActionResultDto>
                {
                    Code    = Code.Success,
                    Message = "Hủy order thành công!",
                    Data    = new OrderActionResultDto
                    {
                        OrderId   = order.Id,
                        OrderCode = order.OrderCode,
                        Status    = order.Status,
                        Message   = "Order đã được hủy.",
                        WasReserved = oldStatus == OrderStatusConstants.Reserved,
                        HadKitchenSentItems = hadKitchenSentItems,
                        Success   = true
                    }
                };
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("SalesOrderService.CancelOrder", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi hệ thống: " + ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════
        // CALCULATE ORDER TOTAL
        // ═══════════════════════════════════════════════════════

        public async Task<Response> CalculateOrderTotal(Guid orderId, string branchCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(branchCode))
                    return new Response(Code.BadRequest, "Mã chi nhánh không được để trống!");

                if (!await ValidateOrderBranch(orderId, branchCode))
                    return new Response(Code.NotFound, "Order không tồn tại hoặc không thuộc chi nhánh này!");

                var orderRepo = _unit.GetRepository<SalesOrder>();
                var order = await orderRepo
                    .Get(x => x.Id == orderId && x.BranchCode == branchCode)
                    .FirstOrDefaultAsync();

                var itemRepo = _unit.GetRepository<SalesOrderItem>();
                var items = await itemRepo
                    .Get(x => x.OrderId == orderId && x.BranchCode == branchCode && x.IsCancelled == 0)
                    .ToListAsync();

                // ── BƯỚC 1: TÍNH LẠI TIỀN HÀNG VÀ DỌN DẸP KHUYẾN MẠI CŨ ───────────────────
                // Chỉ xóa các món tặng tự động phát sinh từ CTKM Tặng món (PromotionType = 2 hoặc có ghi chú món tặng).
                // Các món hàng gốc được chiết khấu 100% (PromotionType = 1) sẽ KHÔNG bị xóa mà được khôi phục đơn giá ban đầu.
                var promoRepo = _unit.GetRepository<Promotion>();
                var currentPromoCodes = items
                    .Where(x => !string.IsNullOrEmpty(x.PromotionCode))
                    .Select(x => x.PromotionCode!)
                    .Distinct()
                    .ToList();

                var promoTypesMap = new Dictionary<string, int>();
                if (currentPromoCodes.Any())
                {
                    promoTypesMap = await promoRepo
                        .Get(x => currentPromoCodes.Contains(x.Code))
                        .ToDictionaryAsync(x => x.Code, x => x.PromotionType);
                }

                var oldGiftItems = items.Where(x => 
                    !string.IsNullOrEmpty(x.PromotionCode) && 
                    x.DiscountRate == 100 && 
                    ((promoTypesMap.TryGetValue(x.PromotionCode, out var pType) && pType == 2) || 
                     (!string.IsNullOrEmpty(x.Note) && x.Note.StartsWith("Món tặng từ CTKM")))
                ).ToList();

                if (oldGiftItems.Any())
                {
                    foreach (var oldGift in oldGiftItems)
                    {
                        itemRepo.Delete(oldGift);
                        items.Remove(oldGift);
                    }
                    await _unit.SaveAsync();
                }

                // Reset giảm giá khuyến mại cũ trên các món ăn chính
                foreach (var item in items)
                {
                    bool isCancelledItem = item.IsCancelled == 1 || (item.Quantity > 0 && ((item.SoldOutQty + item.CancelledQuantity + item.ReturnedQuantity) >= item.Quantity));
                    double billableQty;
                    if (isCancelledItem)
                    {
                        billableQty = 0;
                    }
                    else if (order?.Status == OrderStatusConstants.Checked || item.IsChecked == 1)
                    {
                        billableQty = item.ActualQuantity;
                    }
                    else
                    {
                        // Đơn chưa kiểm đồ (Serving): SL order - SL bếp hết - SL khách trả - SL đã hủy
                        billableQty = Math.Max(0, item.Quantity - item.SoldOutQty - item.ReturnedQuantity - item.CancelledQuantity);
                    }
                    item.Amount = (decimal)billableQty * item.UnitPrice;
                    item.DiscountAmount = 0;
                    item.DiscountRate = 0;
                    item.PromotionCode = null;
                }

                // ── BƯỚC 2: LỌC CÁC CHƯƠNG TRÌNH KHUYẾN MẠI HỢP LỆ ────────────────────────
                var now = DateTime.Now;
                var currentDate = now.Date;
                var currentDOW = (int)now.DayOfWeek;
                var currentTime = now.TimeOfDay;

                var promotions = await promoRepo
                    .Get(x => x.IsActive && x.StartDate <= currentDate && currentDate <= x.EndDate)
                    .ToListAsync();

                var targetBranchCode = order?.BranchCode ?? branchCode;
                var branchRepo = _unit.GetRepository<Branchs>();
                var branch = await branchRepo.Get(x => x.Code == targetBranchCode).FirstOrDefaultAsync();
                var branchId = branch?.Id;

                var customerGroupId = order?.CustomerId.HasValue == true
                    ? (await _unit.GetRepository<BusinessPartner>().Get(x => x.Id == order.CustomerId.Value).Select(x => x.PartnerGroupId).FirstOrDefaultAsync())
                    : null;

                var validPromotions = new List<Promotion>();
                foreach (var p in promotions)
                {
                    if (p.ApplyOnlyToQrOrders && order?.IncludesQrOrder != true) continue;

                    // Check chi nhánh
                    var hasBranch = await _unit.GetRepository<PromotionBranch>().Get(x => x.PromotionId == p.Id).AnyAsync();
                    if (hasBranch && branchId.HasValue)
                    {
                        var matchesBranch = await _unit.GetRepository<PromotionBranch>().Get(x => x.PromotionId == p.Id && x.BranchId == branchId.Value).AnyAsync();
                        if (!matchesBranch) continue;
                    }

                    // Check thứ trong tuần
                    var hasWeekdays = await _unit.GetRepository<PromotionWeekDay>().Get(x => x.PromotionId == p.Id).AnyAsync();
                    if (hasWeekdays)
                    {
                        var matchesWeekday = await _unit.GetRepository<PromotionWeekDay>().Get(x => x.PromotionId == p.Id && x.DayOfWeek == currentDOW).AnyAsync();
                        if (!matchesWeekday) continue;
                    }

                    // Check khung giờ
                    var hasTimeRanges = await _unit.GetRepository<PromotionTimeRange>().Get(x => x.PromotionId == p.Id).AnyAsync();
                    if (hasTimeRanges)
                    {
                        var matchesTime = await _unit.GetRepository<PromotionTimeRange>()
                            .Get(x => x.PromotionId == p.Id && x.StartTime <= currentTime && currentTime <= x.EndTime)
                            .AnyAsync();
                        if (!matchesTime) continue;
                    }

                    // Check nhóm khách hàng
                    var hasCustomerGroups = await _unit.GetRepository<PromotionCustomerGroup>().Get(x => x.PromotionId == p.Id).AnyAsync();
                    if (hasCustomerGroups)
                    {
                        if (!customerGroupId.HasValue) continue;
                        var matchesGroup = await _unit.GetRepository<PromotionCustomerGroup>()
                            .Get(x => x.PromotionId == p.Id && x.CustomerGroupId == customerGroupId.Value)
                            .AnyAsync();
                        if (!matchesGroup) continue;
                    }

                    // Check điều kiện Phiếu trị liệu
                    if (p.RequireTherapyVoucher)
                    {
                        if (string.IsNullOrWhiteSpace(order?.TherapyVoucherCode)) continue;
                        var isValidVoucher = await IsTherapyVoucherValidAsync(order.TherapyVoucherCode);
                        if (!isValidVoucher) continue;
                    }

                    validPromotions.Add(p);
                }

                // Chỉ áp dụng khuyến mại khi thu ngân chọn một mã cụ thể (BE-02)
                if (order != null && !string.IsNullOrWhiteSpace(order.SelectedPromotionCode))
                {
                    validPromotions = validPromotions.Where(x => x.Code == order.SelectedPromotionCode).ToList();
                }
                else
                {
                    validPromotions = new List<Promotion>();
                }

                // ── BƯỚC 3: ĐÁNH GIÁ VÀ CHỌN CTKM TỐT NHẤT ─────────────────────────────────
                var discountItemsConfig = await _unit.GetRepository<PromotionDiscountItem>().GetAll().ToListAsync();
                var discountConfigs = await _unit.GetRepository<PromotionDiscountConfig>().GetAll().ToListAsync();
                var giftConditions = await _unit.GetRepository<PromotionGiftCondition>().GetAll().ToListAsync();
                var giftItems = await _unit.GetRepository<PromotionGiftItem>().GetAll().ToListAsync();
                var menuItems = await _unit.GetRepository<MenuItem>().Get(x => x.Status == 1).ToListAsync();

                Promotion? bestPromo = null;
                decimal bestPromoDiscountValue = 0;
                
                decimal appliedOrderDiscountAmount = 0;
                var appliedDiscountItemUpdates = new Dictionary<Guid, decimal>(); // SalesOrderItemId -> DiscountAmount
                var appliedGiftItemsToInsert = new List<PromotionGiftItem>();

                foreach (var promo in validPromotions)
                {
                    decimal currentPromoDiscountValue = 0;
                    decimal currentOrderDiscountAmount = 0;
                    var currentDiscountItemUpdates = new Dictionary<Guid, decimal>();
                    var currentGiftItemsToInsert = new List<PromotionGiftItem>();

                    if (promo.PromotionType == 1) // Loại Giảm giá
                    {
                        var config = discountConfigs.FirstOrDefault(x => x.PromotionId == promo.Id);
                        if (config != null)
                        {
                            if (config.DiscountScope == 1) // Giảm giá trên đơn hàng
                            {
                                var subTotalTemp = items.Sum(i => i.Amount);
                                if (subTotalTemp >= config.MinOrderAmount)
                                {
                                    decimal discountValue = 0;
                                    foreach (var item in items)
                                    {
                                        var mi = menuItems.FirstOrDefault(m => m.Id == item.ItemId);
                                        var itemTax = CalculateUnitVat(item.UnitPrice, item.VatRate, mi, item.SelectedOptionsJson);
                                        var unitGross = item.UnitPrice + itemTax;

                                        var unitPercentDisc = Math.Round(unitGross * config.DiscountPercent / 100, 0, MidpointRounding.AwayFromZero);
                                        var unitFixedDisc = config.DiscountAmount;
                                        var unitTotalDisc = Math.Min(unitGross, unitPercentDisc + unitFixedDisc);
                                        if (unitTotalDisc < 0) unitTotalDisc = 0;

                                        var itemTotalDisc = Math.Round(unitTotalDisc * (decimal)item.Quantity, 0, MidpointRounding.AwayFromZero);
                                        var itemGrossAmount = Math.Round(unitGross * (decimal)item.Quantity, 0, MidpointRounding.AwayFromZero);
                                        if (itemTotalDisc > itemGrossAmount) itemTotalDisc = itemGrossAmount;

                                        discountValue += itemTotalDisc;
                                    }

                                    var grossTotalTemp = items.Sum(i => {
                                        var mi = menuItems.FirstOrDefault(m => m.Id == i.ItemId);
                                        var itemTax = CalculateUnitVat(i.UnitPrice, i.VatRate, mi, i.SelectedOptionsJson);
                                        return Math.Round((i.UnitPrice + itemTax) * (decimal)i.Quantity, 0, MidpointRounding.AwayFromZero);
                                    });

                                    if (discountValue > grossTotalTemp)
                                    {
                                        discountValue = grossTotalTemp;
                                    }
                                    currentPromoDiscountValue = discountValue;
                                    currentOrderDiscountAmount = discountValue;
                                }
                            }
                            else if (config.DiscountScope == 2) // Giảm giá trên món ăn
                            {
                                var passItems = new List<(SalesOrderItem item, decimal percentDiscount, decimal netAmount)>();

                                foreach (var item in items)
                                {
                                    var discountItemDef = discountItemsConfig.FirstOrDefault(x => x.PromotionId == promo.Id && x.MenuItemId == item.ItemId);
                                    if (discountItemDef == null) continue;

                                    if (item.Amount >= discountItemDef.MinAmount && item.Quantity >= discountItemDef.MinQuantity)
                                    {
                                        var mi = menuItems.FirstOrDefault(m => m.Id == item.ItemId);
                                        var itemTax = (mi != null && mi.FixedTaxAmount > 0)
                                            ? Math.Round(mi.FixedTaxAmount, 0, MidpointRounding.AwayFromZero)
                                            : Math.Round(item.UnitPrice * (decimal)(item.VatRate / 100), 0, MidpointRounding.AwayFromZero);
                                        var unitGross = item.UnitPrice + itemTax;

                                        // Fix rounding: tính discount tại cấp unit trước để tránh double-rounding
                                        decimal unitPercentDiscount = Math.Round(unitGross * config.DiscountPercent / 100, 0, MidpointRounding.AwayFromZero);
                                        decimal percentDiscount = unitPercentDiscount * (decimal)item.Quantity;
                                        var itemGrossAmount = Math.Round(unitGross * (decimal)item.Quantity, 0, MidpointRounding.AwayFromZero);
                                        if (percentDiscount > itemGrossAmount) percentDiscount = itemGrossAmount;
                                        decimal netAmount = itemGrossAmount - percentDiscount;

                                        passItems.Add((item, percentDiscount, netAmount));
                                    }
                                }

                                if (passItems.Any())
                                {
                                    decimal totalItemDiscount = 0;
                                    foreach (var (passItem, percentDiscount, netAmount) in passItems)
                                    {
                                        // For item-scope promotions, the fixed amount applies
                                        // to each qualifying unit, then is capped by the item's
                                        // remaining value after its percentage discount.
                                        var itemCashDiscount = Math.Min(
                                            config.DiscountAmount * (decimal)passItem.Quantity,
                                            netAmount);

                                        decimal itemGross = percentDiscount + netAmount;
                                        decimal itemDiscount = percentDiscount + itemCashDiscount;
                                        if (itemDiscount > itemGross)
                                        {
                                            itemDiscount = itemGross;
                                        }

                                        totalItemDiscount += itemDiscount;
                                        currentDiscountItemUpdates[passItem.Id] = itemDiscount;
                                    }

                                    currentPromoDiscountValue = totalItemDiscount;
                                    if (promo.ApplyOnlyToQrOrders && order?.IncludesQrOrder == true && config.QrOrderDiscountAmount > 0)
                                    {
                                        var remainingOrderAmount = items.Sum(i =>
                                        {
                                            var mi = menuItems.FirstOrDefault(m => m.Id == i.ItemId);
                                            var tax = CalculateUnitVat(i.UnitPrice, i.VatRate, mi, i.SelectedOptionsJson);
                                            var gross = Math.Round((i.UnitPrice + tax) * (decimal)i.Quantity, 0, MidpointRounding.AwayFromZero);
                                            return Math.Max(0, gross - (currentDiscountItemUpdates.TryGetValue(i.Id, out var discount) ? discount : 0));
                                        });
                                        currentOrderDiscountAmount = Math.Min(config.QrOrderDiscountAmount, remainingOrderAmount);
                                        currentPromoDiscountValue += currentOrderDiscountAmount;
                                    }
                                }
                            }
                        }
                    }
                    else if (promo.PromotionType == 2) // Loại Tặng món
                    {
                        var conditions = giftConditions.Where(x => x.PromotionId == promo.Id).ToList();
                        if (conditions.Any())
                        {
                            bool satisfiesAllConditions = true;
                            foreach (var cond in conditions)
                            {
                                var matchingItem = items.FirstOrDefault(i => i.ItemId == cond.MenuItemId);
                                if (matchingItem == null || matchingItem.Quantity < cond.RequiredQuantity)
                                {
                                    satisfiesAllConditions = false;
                                    break;
                                }
                            }

                            if (satisfiesAllConditions)
                            {
                                decimal totalGiftValue = 0;
                                var gifts = giftItems.Where(x => x.PromotionId == promo.Id).ToList();
                                foreach (var gift in gifts)
                                {
                                    var targetMenu = menuItems.FirstOrDefault(x => x.Id == gift.MenuItemId);
                                    if (targetMenu != null)
                                    {
                                        totalGiftValue += (decimal)gift.GiftQuantity * targetMenu.SalePrice;
                                        currentGiftItemsToInsert.Add(gift);
                                    }
                                }
                                currentPromoDiscountValue = totalGiftValue;
                            }
                        }
                    }

                    // Ưu tiên chọn CTKM mang lại tổng trị giá giảm lớn nhất
                    if (currentPromoDiscountValue > bestPromoDiscountValue)
                    {
                        bestPromoDiscountValue = currentPromoDiscountValue;
                        bestPromo = promo;
                        appliedOrderDiscountAmount = currentOrderDiscountAmount;
                        appliedDiscountItemUpdates = currentDiscountItemUpdates;
                        appliedGiftItemsToInsert = currentGiftItemsToInsert;
                    }
                }

                // ── BƯỚC 4: ÁP DỤNG CHÍNH THỨC CTKM ĐƯỢC CHỌN ──────────────────────────────
                var addedGiftItems = new List<SalesOrderItem>();
                if (bestPromo != null)
                {
                    if (bestPromo.PromotionType == 1) // Áp dụng giảm giá
                    {
                        var config = discountConfigs.FirstOrDefault(x => x.PromotionId == bestPromo.Id);
                        if (config != null)
                        {
                            if (config.DiscountScope == 2) // Giảm món
                            {
                                foreach (var item in items)
                                {
                                    if (appliedDiscountItemUpdates.TryGetValue(item.Id, out decimal itemDiscount))
                                    {
                                        item.DiscountAmount = itemDiscount;
                                        item.DiscountRate = (double)config.DiscountPercent;
                                        item.PromotionCode = bestPromo.Code;
                                    }
                                }
                            }
                            else if (config.DiscountScope == 1) // Giảm đơn
                            {
                                foreach (var item in items)
                                {
                                    item.DiscountRate = (double)config.DiscountPercent;
                                    item.PromotionCode = bestPromo.Code;
                                }
                            }
                        }
                    }
                    else if (bestPromo.PromotionType == 2) // Áp dụng tặng món
                    {
                        var orderIdGuid = order?.Id ?? orderId;
                        int maxLineNo = items.Any() ? items.Max(i => i.LineNo) : 0;
                        foreach (var gift in appliedGiftItemsToInsert)
                        {
                            var targetMenu = menuItems.FirstOrDefault(x => x.Id == gift.MenuItemId);
                            if (targetMenu == null) continue;

                            maxLineNo++;
                            var newGiftItem = new SalesOrderItem
                            {
                                Id = Guid.NewGuid(),
                                BranchCode = branchCode,
                                OrderId = orderIdGuid,
                                LineNo = maxLineNo,
                                ItemId = gift.MenuItemId,
                                ItemCode = targetMenu.Code,
                                ItemName = targetMenu.Name,
                                Quantity = gift.GiftQuantity,
                                ActualQuantity = gift.GiftQuantity,
                                UnitPrice = targetMenu.SalePrice,
                                Amount = (decimal)gift.GiftQuantity * targetMenu.SalePrice,
                                DiscountRate = 100,
                                DiscountAmount = (decimal)gift.GiftQuantity * targetMenu.SalePrice, // giảm 100%
                                VatRate = 0,
                                VatAmount = 0,
                                TotalAmount = 0,
                                IsSentToKitchen = 0,
                                IsReturned = 0,
                                IsChecked = 0,
                                IsCancelled = 0,
                                KitchenStatus = "NotSent",
                                PromotionCode = bestPromo.Code,
                                Note = $"Món tặng từ CTKM {bestPromo.Name}",
                                CreatedOnDate = DateTime.Now
                            };

                            itemRepo.Add(newGiftItem);
                            addedGiftItems.Add(newGiftItem);
                        }

                        if (addedGiftItems.Any())
                        {
                            await _unit.SaveAsync();
                            items.AddRange(addedGiftItems);
                        }
                    }
                }

                var step5MenuItemRepo = _unit.GetRepository<MenuItem>();
                var step5MenuItemIds = items.Select(x => x.ItemId).Distinct().ToList();
                var step5MenuItems = await step5MenuItemRepo.Get(x => step5MenuItemIds.Contains(x.Id)).ToListAsync();
                var step5MenuItemDict = step5MenuItems.ToDictionary(x => x.Id, x => x);

                var totalNetItemsAmount = items.Sum(i => {
                    var mi = step5MenuItemDict.TryGetValue(i.ItemId, out var m) ? m : null;
                    var itemTax = CalculateUnitVat(i.UnitPrice, i.VatRate, mi, i.SelectedOptionsJson);
                    var itemGross = Math.Round((i.UnitPrice + itemTax) * (decimal)i.Quantity, 0, MidpointRounding.AwayFromZero);
                    return Math.Max(0, itemGross - i.DiscountAmount);
                });

                var bestPromoConfig = (bestPromo != null && bestPromo.PromotionType == 1) 
                    ? discountConfigs.FirstOrDefault(x => x.PromotionId == bestPromo.Id) 
                    : null;
                var remainingOrderDiscountAmount = Math.Min(appliedOrderDiscountAmount, totalNetItemsAmount);
                var remainingOrderDiscountBaseAmount = totalNetItemsAmount;

                foreach (var item in items)
                {
                    item.Amount = Math.Round(item.Amount, 0, MidpointRounding.AwayFromZero);

                    var step5Mi = step5MenuItemDict.TryGetValue(item.ItemId, out var m) ? m : null;
                    var itemTax = CalculateUnitVat(item.UnitPrice, item.VatRate, step5Mi, item.SelectedOptionsJson);
                    var unitGross = item.UnitPrice + itemTax;
                    var itemGrossAmount = Math.Round(unitGross * (decimal)item.Quantity, 0, MidpointRounding.AwayFromZero);

                    decimal itemOrderDiscount = 0;
                    if (appliedOrderDiscountAmount > 0)
                    {
                        var netItemAmount = Math.Max(0, itemGrossAmount - item.DiscountAmount);
                        if (bestPromoConfig != null && bestPromoConfig.DiscountScope == 1)
                        {
                            var unitPercentDisc = Math.Round(unitGross * bestPromoConfig.DiscountPercent / 100, 0, MidpointRounding.AwayFromZero);
                            var unitFixedDisc = bestPromoConfig.DiscountAmount;
                            var unitTotalDisc = Math.Min(unitGross, unitPercentDisc + unitFixedDisc);
                            if (unitTotalDisc < 0) unitTotalDisc = 0;

                            var lineDiscount = Math.Round(unitTotalDisc * (decimal)item.Quantity, 0, MidpointRounding.AwayFromZero);
                            itemOrderDiscount = Math.Min(netItemAmount, lineDiscount);
                            if (itemOrderDiscount < 0) itemOrderDiscount = 0;
                        }
                        else if (totalNetItemsAmount > 0)
                        {
                            // Allocate against the remaining amount and base so rounded
                            // line shares add up to the configured order discount exactly.
                            if (remainingOrderDiscountAmount > 0 && remainingOrderDiscountBaseAmount > 0)
                            {
                                itemOrderDiscount = netItemAmount >= remainingOrderDiscountBaseAmount
                                    ? remainingOrderDiscountAmount
                                    : Math.Round(remainingOrderDiscountAmount * (netItemAmount / remainingOrderDiscountBaseAmount), 0, MidpointRounding.AwayFromZero);
                                itemOrderDiscount = Math.Min(itemOrderDiscount, Math.Min(netItemAmount, remainingOrderDiscountAmount));
                                remainingOrderDiscountAmount -= itemOrderDiscount;
                                remainingOrderDiscountBaseAmount = Math.Max(0, remainingOrderDiscountBaseAmount - netItemAmount);
                            }
                        }
                    }

                    if (itemOrderDiscount > 0 && bestPromo != null)
                    {
                        // The QR order discount is part of this promotion even on lines
                        // that are not individually eligible for its item discount.
                        item.PromotionCode = bestPromo.Code;
                    }
                    item.DiscountAmount = Math.Round(item.DiscountAmount + itemOrderDiscount, 0, MidpointRounding.AwayFromZero);

                    bool isCancelledItem = item.IsCancelled == 1 || (item.Quantity > 0 && ((item.SoldOutQty + item.CancelledQuantity + item.ReturnedQuantity) >= item.Quantity));
                    var qtyDouble = isCancelledItem
                        ? 0
                        : ((order?.Status == OrderStatusConstants.Checked || item.IsChecked == 1)
                            ? item.ActualQuantity
                            : Math.Max(0, item.Quantity - item.SoldOutQty - item.ReturnedQuantity - item.CancelledQuantity));
                    var qty = (decimal)qtyDouble;

                    if (isCancelledItem || qty <= 0)
                    {
                        item.Amount = 0;
                        item.DiscountAmount = 0;
                        item.DiscountRate = 0;
                        item.VatAmount = 0;
                        item.TotalAmount = 0;
                        itemRepo.Update(item);
                        continue;
                    }

                    var effectiveQty = qty > 0 ? qty : 1m;
                    var totalDiscount = item.DiscountAmount;
                    var unitDiscount = Math.Round(totalDiscount / effectiveQty, 0, MidpointRounding.AwayFromZero);
                    var unitTotal = Math.Max(0, unitGross - unitDiscount);

                    if (unitTotal <= 0 || item.DiscountRate >= 100)
                    {
                        unitTotal = 0;
                        item.DiscountAmount = Math.Round(unitGross * effectiveQty, 0, MidpointRounding.AwayFromZero);
                        item.VatAmount = 0;
                        item.TotalAmount = 0;
                    }
                    else
                    {
                        item.DiscountAmount = Math.Round(unitDiscount * effectiveQty, 0, MidpointRounding.AwayFromZero);
                        item.VatAmount = Math.Round(itemTax * qty, 0, MidpointRounding.AwayFromZero);
                        item.TotalAmount = Math.Round(unitTotal * qty, 0, MidpointRounding.AwayFromZero);
                    }
                    itemRepo.Update(item);
                }

                // Per-unit rounding above can make the distributed QR order discount
                // differ from its configured amount. Correct a line-level share while
                // keeping the persisted line totals and order totals in sync.
                if (bestPromoConfig?.DiscountScope == 2 && appliedOrderDiscountAmount > 0)
                {
                    var targetPromoDiscount = appliedDiscountItemUpdates.Values.Sum() + appliedOrderDiscountAmount;
                    var actualPromoDiscount = items.Sum(i => i.DiscountAmount);
                    var roundingDifference = actualPromoDiscount - targetPromoDiscount;
                    if (roundingDifference != 0)
                    {
                        var roundingLine = items.FirstOrDefault(i => i.IsCancelled == 0 && i.PromotionCode == bestPromo!.Code);
                        if (roundingLine != null)
                        {
                            var adjustedDiscount = roundingLine.DiscountAmount - roundingDifference;
                            var quantity = (decimal)Math.Max(0, roundingLine.Quantity - roundingLine.SoldOutQty - roundingLine.ReturnedQuantity - roundingLine.CancelledQuantity);
                            var lineGross = (roundingLine.UnitPrice + (roundingLine.VatAmount / Math.Max(1m, quantity))) * quantity;
                            if (adjustedDiscount >= 0 && adjustedDiscount <= lineGross)
                            {
                                var totalDelta = roundingDifference;
                                roundingLine.DiscountAmount = adjustedDiscount;
                                roundingLine.TotalAmount = Math.Max(0, roundingLine.TotalAmount + totalDelta);
                                itemRepo.Update(roundingLine);
                            }
                        }
                    }
                }

                var finalSubTotal = items.Sum(i => Math.Round(i.Amount, 0, MidpointRounding.AwayFromZero));
                var finalDiscountAmount = items.Sum(i => Math.Round(i.DiscountAmount, 0, MidpointRounding.AwayFromZero));
                var finalVatAmount = items.Sum(i => Math.Round(i.VatAmount, 0, MidpointRounding.AwayFromZero));
                // Dùng sum(item.TotalAmount) để nhất quán với SaveOrder, AddOrUpdateOrderItem
                var finalTotalAmount = items.Sum(i => Math.Round(i.TotalAmount, 0, MidpointRounding.AwayFromZero));

                if (finalTotalAmount <= 0)
                {
                    finalTotalAmount = 0;
                    finalVatAmount = 0;
                    foreach (var item in items)
                    {
                        if (item.VatAmount > 0)
                        {
                            item.VatAmount = 0;
                            itemRepo.Update(item);
                        }
                    }
                }

                order!.SubTotal = finalSubTotal;
                order.DiscountAmount = finalDiscountAmount;
                order.VatAmount = finalVatAmount;
                order.TotalAmount = finalTotalAmount;
                order.UpdatedOnDate = DateTime.Now;


                orderRepo.Update(order);

                // ── BƯỚC 5.1: CẬP NHẬT/TẠO DÒNG BẢN GHI THANH TOÁN KHUYẾN MẠI (BE-07) ──────────────
                var paymentRepo = _unit.GetRepository<SalesOrderPayment>();
                var existingPromoPayment = await paymentRepo
                    .Get(x => x.OrderId == order.Id && x.BranchCode == branchCode && x.PaymentType == "Tiền khuyến mãi")
                    .FirstOrDefaultAsync();

                if (finalDiscountAmount > 0)
                {
                    if (existingPromoPayment != null)
                    {
                        existingPromoPayment.PaymentAmount = -finalDiscountAmount;
                        existingPromoPayment.CashReceivedAmount = 0;
                        existingPromoPayment.ChangeAmount = 0;
                        existingPromoPayment.CreatedOnDate = DateTime.Now;
                        paymentRepo.Update(existingPromoPayment);
                    }
                    else
                    {
                        var newPromoPayment = new SalesOrderPayment
                        {
                            Id = Guid.NewGuid(),
                            BranchCode = branchCode,
                            OrderId = order.Id,
                            PaymentType = "Tiền khuyến mãi",
                            PaymentAmount = -finalDiscountAmount,
                            CashReceivedAmount = 0,
                            ChangeAmount = 0,
                            CreatedOnDate = DateTime.Now
                        };
                        paymentRepo.Add(newPromoPayment);
                    }
                }
                else
                {
                    if (existingPromoPayment != null)
                    {
                        paymentRepo.Delete(existingPromoPayment);
                    }
                }

                await _unit.SaveAsync();

                var tableRepo = _unit.GetRepository<SalesOrderTable>();
                var tables = await tableRepo
                    .Get(x => x.OrderId == order.Id && x.BranchCode == branchCode)
                    .ToListAsync();

                var payments = await paymentRepo
                    .Get(x => x.OrderId == order.Id && x.BranchCode == branchCode)
                    .ToListAsync();

                return new ResponseObject<SalesOrderDto>
                {
                    Code = Code.Success,
                    Message = bestPromo != null 
                        ? $"Áp dụng chương trình khuyến mại '{bestPromo.Name}' và tính lại tổng tiền thành công!" 
                        : "Tính lại tổng tiền hóa đơn thành công!",
                    Data = await MapOrderToDtoAsync(order, items, tables, payments)
                };
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("SalesOrderService.CalculateOrderTotal", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi hệ thống: " + ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════
        // VALIDATE HELPERS
        // ═══════════════════════════════════════════════════════

        public async Task<bool> ValidateOrderBranch(Guid orderId, string branchCode)
        {
            if (orderId == Guid.Empty || string.IsNullOrWhiteSpace(branchCode))
                return false;

            var orderRepo = _unit.GetRepository<SalesOrder>();
            return await orderRepo
                .Get(x => x.Id == orderId && x.BranchCode == branchCode)
                .AnyAsync();
        }

        public async Task<bool> ValidateTableBranch(Guid tableId, string branchCode)
        {
            if (tableId == Guid.Empty || string.IsNullOrWhiteSpace(branchCode))
                return false;

            var branchRepo = _unit.GetRepository<Branchs>();
            var branch = await branchRepo
                .Get(x => x.Code == branchCode && x.Status != 0)
                .FirstOrDefaultAsync();
            if (branch == null) return false;

            var tableRepo = _unit.GetRepository<DiningTable>();
            return await tableRepo
                .Get(x => x.Id == tableId && x.BranchId == branch.Id && x.Status != 0)
                .AnyAsync();
        }

        // ═══════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ═══════════════════════════════════════════════════════

        private async Task<Response> CreateNewOrder(
            CreateOrUpdateOrderRequest request,
            Branchs branch,
            IRepository<SalesOrder> orderRepo,
            IRepository<SalesOrderItem> itemRepo,
            IRepository<SalesOrderTable> orderTableRepo,
            string? userCode = null,
            string? userName = null)
        {
            string initialStatus = string.IsNullOrWhiteSpace(request.Status)
                ? OrderStatusConstants.Serving
                : request.Status;

            if (!OrderStatusConstants.CanTransition(null, initialStatus, "Save"))
            {
                return new Response(Code.BadRequest, $"Không thể chuyển trạng thái sang {initialStatus}!");
            }

            var now       = DateTime.Now;
            var openTime = request.OpenTime ?? request.OrderDate ?? now;
            if (openTime.Kind == DateTimeKind.Utc) openTime = openTime.ToLocalTime();
            var orderId   = Guid.NewGuid();
            var orderCode = await GenerateOrderCode(request.BranchCode, openTime);

            // Lấy FloorId từ bàn đầu tiên (nếu DineIn)
            Guid floorId   = Guid.Empty;
            string? floorName = null;
            bool isDineIn  = request.ServiceType == ServiceTypeConstants.DineIn;

            if (isDineIn && request.TableIds.Any())
            {
                var tableRepo  = _unit.GetRepository<DiningTable>();
                var firstTable = await tableRepo
                    .Get(x => x.Id == request.TableIds.First() && x.Status != 0)
                    .FirstOrDefaultAsync();
                if (firstTable != null)
                {
                    floorId = firstTable.FloorPlanId;

                    var floorRepo = _unit.GetRepository<FloorPlan>();
                    var floor     = await floorRepo
                        .Get(x => x.Id == floorId)
                        .FirstOrDefaultAsync();
                    floorName = floor?.Name;
                }
            }

            // Tạo order
            var order = new SalesOrder
            {
                Id               = orderId,
                BranchCode       = request.BranchCode,
                OrderCode        = orderCode,
                OrderDate        = openTime.Date,
                OpenTime         = openTime,
                WaiterId         = request.WaiterId,
                WaiterName       = request.WaiterName,
                CustomerId       = request.CustomerId,
                CustomerCode     = request.CustomerCode,
                CustomerName     = request.CustomerName,
                PhoneNumber      = request.PhoneNumber,
                FloorId          = floorId,
                FloorName        = floorName,
                CustomerCount    = request.CustomerCount,
                ServiceType      = string.IsNullOrWhiteSpace(request.ServiceType)
                                   ? ServiceTypeConstants.DineIn
                                   : request.ServiceType,
                Status           = initialStatus,
                DeliveryAddress  = request.DeliveryAddress,
                DeliveryNote     = request.DeliveryNote,
                ShippingFeeNote  = request.ShippingFeeNote,
                Note             = request.Note,
                SelectedPromotionCode = request.SelectedPromotionCode,
                TherapyVoucherCode = request.TherapyVoucherCode,
                CreatedOnDate    = now,
                CreatedByUserCode = !string.IsNullOrEmpty(userCode) ? userCode : "system",
                CreatedByUserName = !string.IsNullOrEmpty(userName) ? userName : "System",
                UpdatedOnDate    = now,
                UpdatedByUserCode = userCode,
                UpdatedByUserName = userName
            };
            orderRepo.Add(order);

            // Tạo order-table links
            if (isDineIn)
            {
                var tableRepo = _unit.GetRepository<DiningTable>();
                foreach (var tableId in request.TableIds)
                {
                    var table = await tableRepo
                        .Get(x => x.Id == tableId && x.Status != 0)
                        .FirstOrDefaultAsync();
                    if (table == null) continue;

                    orderTableRepo.Add(new SalesOrderTable
                    {
                        Id            = Guid.NewGuid(),
                        BranchCode    = request.BranchCode,
                        OrderId       = orderId,
                        TableId       = tableId,
                        TableCode     = table.Code,
                        TableName     = table.Name,
                        FloorId       = table.FloorPlanId,
                        CreatedOnDate = now
                    });
                }
            }

            // Tạo order items
            bool requireKitchen = await IsRequireKitchenSendEnabled();
            bool trackKitchenSend = requireKitchen || await IsKitchenPrintEnabled();
            var lineNo = 1;
            decimal subTotal = 0, discountTotal = 0, vatTotal = 0;
            var menuItemRepoForCreate = _unit.GetRepository<MenuItem>();
            var createItemIds = request.Items.Select(x => x.ItemId).Distinct().ToList();
            var createMenuItems = await menuItemRepoForCreate.Get(x => createItemIds.Contains(x.Id)).ToListAsync();
            var createMenuItemDict = createMenuItems.ToDictionary(x => x.Id, x => x);

            // Kiểm tra số lượng tồn kho trước khi tạo order
            foreach (var req in request.Items)
            {
                if (createMenuItemDict.TryGetValue(req.ItemId, out var checkMi) && checkMi.IsTrackStock)
                {
                    int available = checkMi.StockQuantity ?? 0;
                    if (available < req.Quantity)
                    {
                        return new Response(Code.BadRequest, $"Món '{checkMi.Name}' chỉ còn lại {available} suất, không đủ số lượng yêu cầu ({req.Quantity})!");
                    }
                }
            }

            foreach (var req in request.Items)
            {
                var qty = (decimal)req.Quantity;
                var unitPrice = req.UnitPrice;

                // Trừ tồn kho nếu món có quản lý tồn
                if (createMenuItemDict.TryGetValue(req.ItemId, out var miStock) && miStock.IsTrackStock)
                {
                    miStock.StockQuantity = (miStock.StockQuantity ?? 0) - (int)req.Quantity;
                    if (miStock.StockQuantity <= 0)
                    {
                        miStock.StockQuantity = 0;
                        miStock.IsAvailable = false;
                        miStock.DisabledAt = DateTime.UtcNow;
                    }
                    menuItemRepoForCreate.Update(miStock);
                }

                createMenuItemDict.TryGetValue(req.ItemId, out var mi);
                var unitVat = CalculateUnitVat(unitPrice, req.VatRate, mi, req.SelectedOptionsJson);
                var unitGross = unitPrice + unitVat;
                var unitDiscount = req.DiscountRate > 0 ? Math.Round(unitGross * (decimal)(req.DiscountRate / 100), 0, MidpointRounding.AwayFromZero) : 0m;
                var unitTotal = Math.Max(0, unitGross - unitDiscount);

                decimal vat = 0;
                if (req.DiscountRate >= 100 || unitTotal <= 0)
                {
                    unitTotal = 0;
                    unitDiscount = unitGross;
                    vat = 0;
                }
                else
                {
                    vat = Math.Round(qty * unitVat, 0, MidpointRounding.AwayFromZero);
                }

                var amount = Math.Round(qty * unitPrice, 0, MidpointRounding.AwayFromZero);
                var discount = Math.Round(qty * unitDiscount, 0, MidpointRounding.AwayFromZero);
                var total = Math.Round(qty * unitTotal, 0, MidpointRounding.AwayFromZero);

                subTotal     += amount;
                discountTotal += discount;
                vatTotal      += vat;

                itemRepo.Add(new SalesOrderItem
                {
                    Id              = Guid.NewGuid(),
                    BranchCode      = request.BranchCode,
                    OrderId         = orderId,
                    LineNo          = lineNo++,
                    ItemId          = req.ItemId,
                    ItemCode        = req.ItemCode,
                    ItemName        = req.ItemName,
                    UnitName        = req.UnitName,
                    Quantity        = req.Quantity,
                    SentQty         = trackKitchenSend ? 0 : req.Quantity,
                    ServedQty       = trackKitchenSend ? 0 : req.Quantity,
                    RemainingQty    = trackKitchenSend ? req.Quantity : 0,
                    KitchenStatus   = trackKitchenSend ? "NotSent" : "FullyServed",
                    ActualQuantity  = 0,
                    ReturnedQuantity = 0,
                    CancelledQuantity = 0,
                    UnitPrice       = req.UnitPrice,
                    Amount          = amount,
                    DiscountRate    = req.DiscountRate,
                    DiscountAmount  = discount,
                    VatRate         = req.VatRate,
                    VatAmount       = vat,
                    TotalAmount     = total,
                    IsSentToKitchen = trackKitchenSend ? 0 : 1,
                    IsReturned      = 0,
                    IsChecked       = 0,
                    IsCancelled     = 0,
                    PromotionCode   = req.PromotionCode,
                    Note            = req.Note,
                    SelectedOptionsJson = req.SelectedOptionsJson,
                    CreatedOnDate   = now,
                    UpdatedOnDate   = now
                });
            }

            // Cập nhật tổng tiền vào order
            order.SubTotal      = subTotal;
            order.DiscountAmount = discountTotal;
            order.VatAmount     = vatTotal;
            order.TotalAmount   = Math.Max(0, subTotal - discountTotal + vatTotal);
            if (order.TotalAmount == 0)
            {
                order.VatAmount = 0;
            }

            await _unit.SaveAsync();

            await _logger.InfoAsync($"Order status changed: OrderId={orderId}, BranchCode={request.BranchCode}, Action=Save, OldStatus=Draft, NewStatus={initialStatus}");

            await BroadcastSalesEventAsync(request.BranchCode, new SalesRealtimeEventDto
            {
                EventType = "TableOpened",
                OrderId = orderId,
                OrderCode = orderCode,
                NewStatus = initialStatus,
                ServiceType = order.ServiceType
            });

            return new ResponseObject<OrderActionResultDto>
            {
                Code    = Code.Success,
                Message = "Tạo order thành công!",
                Data    = new OrderActionResultDto
                {
                    OrderId   = orderId,
                    OrderCode = orderCode,
                    Status    = initialStatus,
                    Message   = "Order đã được tạo mới.",
                    Success   = true
                }
            };
        }

        private async Task<Response> UpdateExistingOrder(
            CreateOrUpdateOrderRequest request,
            IRepository<SalesOrder> orderRepo,
            IRepository<SalesOrderItem> itemRepo,
            IRepository<SalesOrderTable> orderTableRepo,
            string? userCode = null,
            string? userName = null)
        {
            var orderId = request.OrderId!.Value;
            var now     = DateTime.Now;

            var order = await orderRepo
                .Get(x => x.Id == orderId && x.BranchCode == request.BranchCode)
                .FirstOrDefaultAsync();

            if (order == null)
                return new Response(Code.NotFound, "Order không tồn tại!");

            // Không cập nhật order đã kết thúc
            if (!OrderStatusConstants.IsActive(order.Status))
                return new Response(Code.BadRequest, $"Không thể sửa order ở trạng thái '{order.Status}'!");

            // Cập nhật thông tin order
            order.WaiterId        = request.WaiterId      ?? order.WaiterId;
            order.WaiterName      = request.WaiterName    ?? order.WaiterName;
            order.CustomerId      = request.CustomerIdSpecified ? request.CustomerId : request.CustomerId ?? order.CustomerId;
            order.CustomerCode    = request.CustomerCode  ?? order.CustomerCode;
            order.CustomerName    = request.CustomerName  ?? order.CustomerName;
            order.PhoneNumber     = request.PhoneNumber   ?? order.PhoneNumber;
            order.CustomerCount   = request.CustomerCount > 0 ? request.CustomerCount : order.CustomerCount;
            order.DeliveryAddress = request.DeliveryAddress ?? order.DeliveryAddress;
            order.DeliveryNote    = request.DeliveryNote    ?? order.DeliveryNote;
            order.ShippingFeeNote = request.ShippingFeeNote ?? order.ShippingFeeNote;
            order.Note            = request.Note            ?? order.Note;
            order.SelectedPromotionCode = request.SelectedPromotionCode;
            order.TherapyVoucherCode = request.TherapyVoucherCode;
            if (request.OpenTime.HasValue)
            {
                var ot = request.OpenTime.Value;
                if (ot.Kind == DateTimeKind.Utc) ot = ot.ToLocalTime();
                order.OpenTime = ot;
                order.OrderDate = ot.Date;
            }
            else if (request.OrderDate.HasValue)
            {
                var od = request.OrderDate.Value;
                if (od.Kind == DateTimeKind.Utc) od = od.ToLocalTime();
                order.OrderDate = od.Date;
            }
            order.UpdatedOnDate   = now;
            if (!string.IsNullOrEmpty(userCode)) order.UpdatedByUserCode = userCode;
            if (!string.IsNullOrEmpty(userName)) order.UpdatedByUserName = userName;

            // Xóa items cũ chưa gửi bếp, thêm/cập nhật items mới
            var existingItems = await itemRepo
                .Get(x => x.OrderId == orderId && x.BranchCode == request.BranchCode)
                .ToListAsync();

            bool isModified = false;
            bool cancelledSentItem = false;

            bool requireKitchenUpdate = await IsRequireKitchenSendEnabled();
            bool trackKitchenUpdate = requireKitchenUpdate || await IsKitchenPrintEnabled();

            // Find items that are in DB but missing in request payload (deleted on UI)
            var requestedItemIds = request.Items.Where(i => i.Id.HasValue && i.Id.Value != Guid.Empty).Select(i => i.Id!.Value).ToList();
            var itemsToRemove = existingItems.Where(i => !requestedItemIds.Contains(i.Id) && i.IsCancelled == 0).ToList();

            if (itemsToRemove.Any())
            {
                isModified = true;
            }
            var menuItemRepoForRemove = _unit.GetRepository<MenuItem>();
            foreach (var itemToRemove in itemsToRemove)
            {
                // Hoàn lại số lượng tồn kho khi xóa món khỏi đơn
                var miToRemove = await menuItemRepoForRemove.Get(x => x.Id == itemToRemove.ItemId).FirstOrDefaultAsync();
                if (miToRemove != null && miToRemove.IsTrackStock)
                {
                    miToRemove.StockQuantity = (miToRemove.StockQuantity ?? 0) + (int)itemToRemove.Quantity;
                    if (miToRemove.StockQuantity > 0)
                    {
                        miToRemove.IsAvailable = true;
                        miToRemove.DisabledAt = null;
                    }
                    menuItemRepoForRemove.Update(miToRemove);
                }

                if (itemToRemove.IsSentToKitchen == 1)
                {
                    cancelledSentItem = true;
                    itemToRemove.IsCancelled = 1;
                    itemToRemove.KitchenStatus = "Cancelled";
                    itemToRemove.RemainingQty = 0;
                    itemToRemove.UpdatedOnDate = now;
                    itemRepo.Update(itemToRemove);

                    // Cancel pending kitchen requests for this item
                    var pendingDetails = await _unit.GetRepository<KitchenRequestDetail>()
                        .Get(x => x.OrderDetailId == itemToRemove.Id && x.RemainingQty > 0)
                        .ToListAsync();
                    foreach (var pd in pendingDetails)
                    {
                        pd.RemainingQty = 0;
                        pd.Status = "Completed";
                        _unit.GetRepository<KitchenRequestDetail>().Update(pd);
                    }
                }
                else
                {
                    itemRepo.Delete(itemToRemove);
                    existingItems.Remove(itemToRemove);
                }
            }

            var maxLineNo = existingItems.Any() ? existingItems.Max(i => i.LineNo) : 0;
            var lineNo    = maxLineNo + 1;

            decimal subTotal = existingItems
                .Where(i => i.IsCancelled == 0)
                .Sum(i => i.Amount);
            decimal discountTotal = existingItems
                .Where(i => i.IsCancelled == 0)
                .Sum(i => i.DiscountAmount);
            decimal vatTotal = existingItems
                .Where(i => i.IsCancelled == 0)
                .Sum(i => i.VatAmount);

            foreach (var req in request.Items)
            {
                if (req.Id.HasValue && req.Id != Guid.Empty)
                {
                    // Cập nhật item đã tồn tại
                    var existing = existingItems.FirstOrDefault(i => i.Id == req.Id.Value);
                    if (existing != null && existing.IsCancelled == 0)
                    {
                        if (!string.IsNullOrWhiteSpace(existing.SelectedOptionsJson))
                            req.UnitPrice = existing.UnitPrice;
                        if (existing.IsSentToKitchen == 1 && req.Quantity < existing.Quantity)
                            cancelledSentItem = true;
                        if (existing.Quantity != req.Quantity || 
                            existing.UnitPrice != req.UnitPrice || 
                            existing.DiscountRate != req.DiscountRate || 
                            existing.VatRate != req.VatRate ||
                            existing.Note != req.Note)
                        {
                            isModified = true;
                        }

                        var oldAmount   = existing.Amount;
                        var oldDiscount = existing.DiscountAmount;
                        var oldVat      = existing.VatAmount;

                        existing.Quantity      = req.Quantity;
                        existing.UnitPrice     = req.UnitPrice;
                        existing.DiscountRate  = req.DiscountRate;
                        existing.VatRate       = req.VatRate;
                        if (!trackKitchenUpdate)
                        {
                            existing.IsSentToKitchen = 1;
                            existing.SentQty = req.Quantity;
                            existing.ServedQty = Math.Max(0, req.Quantity - existing.CancelledQuantity - existing.SoldOutQty);
                            existing.RemainingQty = 0;
                            existing.KitchenStatus = req.Quantity <= 0 ? "NotSent" : "FullyServed";
                        }
                        var qty = (decimal)req.Quantity;
                        var unitPrice = req.UnitPrice;

                        var updateMenuItemRepo = _unit.GetRepository<MenuItem>();
                        var existingMi = await updateMenuItemRepo.Get(x => x.Id == req.ItemId).FirstOrDefaultAsync();

                        // Kiểm tra chênh lệch số lượng và cập nhật tồn kho
                        if (existingMi != null && existingMi.IsTrackStock && req.Quantity != existing.Quantity)
                        {
                            var diff = req.Quantity - existing.Quantity;
                            if (diff > 0)
                            {
                                int available = existingMi.StockQuantity ?? 0;
                                if (available < diff)
                                    return new Response(Code.BadRequest, $"Món '{existingMi.Name}' chỉ còn lại {available} suất, không đủ số lượng tăng thêm ({diff})!");
                                existingMi.StockQuantity = available - (int)diff;
                                if (existingMi.StockQuantity <= 0)
                                {
                                    existingMi.StockQuantity = 0;
                                    existingMi.IsAvailable = false;
                                    existingMi.DisabledAt = DateTime.UtcNow;
                                }
                            }
                            else
                            {
                                int refund = (int)(-diff);
                                existingMi.StockQuantity = (existingMi.StockQuantity ?? 0) + refund;
                                if (existingMi.StockQuantity > 0)
                                {
                                    existingMi.IsAvailable = true;
                                    existingMi.DisabledAt = null;
                                }
                            }
                            updateMenuItemRepo.Update(existingMi);
                        }

                        var unitVat = CalculateUnitVat(unitPrice, req.VatRate, existingMi, req.SelectedOptionsJson ?? existing.SelectedOptionsJson);
                        var unitGross = unitPrice + unitVat;
                        var unitDiscount = req.DiscountRate > 0 ? Math.Round(unitGross * (decimal)(req.DiscountRate / 100), 0, MidpointRounding.AwayFromZero) : 0m;
                        var unitTotal = Math.Max(0, unitGross - unitDiscount);

                        decimal existingVat = 0;
                        if (req.DiscountRate >= 100 || unitTotal <= 0)
                        {
                            unitTotal = 0;
                            unitDiscount = unitGross;
                            existingVat = 0;
                        }
                        else
                        {
                            existingVat = Math.Round(qty * unitVat, 0, MidpointRounding.AwayFromZero);
                        }

                        existing.Amount = Math.Round(qty * unitPrice, 0, MidpointRounding.AwayFromZero);
                        existing.DiscountAmount = Math.Round(qty * unitDiscount, 0, MidpointRounding.AwayFromZero);
                        existing.VatAmount = existingVat;
                        existing.TotalAmount = Math.Round(qty * unitTotal, 0, MidpointRounding.AwayFromZero);
                        existing.Note          = req.Note ?? existing.Note;
                        existing.PromotionCode = req.PromotionCode ?? existing.PromotionCode;
                        existing.UpdatedOnDate = now;

                        subTotal      += existing.Amount - oldAmount;
                        discountTotal += existing.DiscountAmount - oldDiscount;
                        vatTotal      += existing.VatAmount - oldVat;

                        itemRepo.Update(existing);
                    }
                }
                else
                {
                    // Thêm item mới
                    var reqQty = (decimal)req.Quantity;
                    var reqUnitPrice = req.UnitPrice;

                    var addMenuItemRepo = _unit.GetRepository<MenuItem>();
                    var addMi = await addMenuItemRepo.Get(x => x.Id == req.ItemId).FirstOrDefaultAsync();

                    if (addMi != null && addMi.IsTrackStock)
                    {
                        int available = addMi.StockQuantity ?? 0;
                        if (available < req.Quantity)
                            return new Response(Code.BadRequest, $"Món '{addMi.Name}' chỉ còn lại {available} suất, không đủ số lượng yêu cầu ({req.Quantity})!");
                        addMi.StockQuantity = available - (int)req.Quantity;
                        if (addMi.StockQuantity <= 0)
                        {
                            addMi.StockQuantity = 0;
                            addMi.IsAvailable = false;
                            addMi.DisabledAt = DateTime.UtcNow;
                        }
                        addMenuItemRepo.Update(addMi);
                    }

                    var unitVat1602 = CalculateUnitVat(reqUnitPrice, req.VatRate, addMi, req.SelectedOptionsJson);
                    var unitGross1602 = reqUnitPrice + unitVat1602;
                    var unitDisc1602 = req.DiscountRate > 0 ? Math.Round(unitGross1602 * (decimal)(req.DiscountRate / 100), 0, MidpointRounding.AwayFromZero) : 0m;
                    var unitTotal1602 = Math.Max(0, unitGross1602 - unitDisc1602);

                    decimal newVat = 0;
                    if (req.DiscountRate >= 100 || unitTotal1602 <= 0)
                    {
                        unitTotal1602 = 0;
                        unitDisc1602 = unitGross1602;
                        newVat = 0;
                    }
                    else
                    {
                        newVat = Math.Round(reqQty * unitVat1602, 0, MidpointRounding.AwayFromZero);
                    }

                    var amount   = Math.Round(reqQty * reqUnitPrice, 0, MidpointRounding.AwayFromZero);
                    var discount = Math.Round(reqQty * unitDisc1602, 0, MidpointRounding.AwayFromZero);
                    var vat      = newVat;
                    var total    = Math.Round(reqQty * unitTotal1602, 0, MidpointRounding.AwayFromZero);

                    subTotal      += amount;
                    discountTotal += discount;
                    vatTotal      += vat;

                    itemRepo.Add(new SalesOrderItem
                    {
                        Id                = Guid.NewGuid(),
                        BranchCode        = request.BranchCode,
                        OrderId           = orderId,
                        LineNo            = lineNo++,
                        ItemId            = req.ItemId,
                        ItemCode          = req.ItemCode,
                        ItemName          = req.ItemName,
                        UnitName          = req.UnitName,
                        Quantity          = req.Quantity,
                        SentQty           = trackKitchenUpdate ? 0 : req.Quantity,
                        ServedQty         = trackKitchenUpdate ? 0 : req.Quantity,
                        RemainingQty      = trackKitchenUpdate ? req.Quantity : 0,
                        KitchenStatus     = trackKitchenUpdate ? "NotSent" : "FullyServed",
                        ActualQuantity    = 0,
                        ReturnedQuantity  = 0,
                        CancelledQuantity = 0,
                        UnitPrice         = req.UnitPrice,
                        Amount            = amount,
                        DiscountRate      = req.DiscountRate,
                        DiscountAmount    = discount,
                        VatRate           = req.VatRate,
                        VatAmount         = vat,
                        TotalAmount       = total,
                        IsSentToKitchen   = trackKitchenUpdate ? 0 : 1,
                        IsReturned        = 0,
                        IsChecked         = 0,
                        IsCancelled       = 0,
                        PromotionCode     = req.PromotionCode,
                        Note              = req.Note,
                        SelectedOptionsJson = req.SelectedOptionsJson,
                        CreatedOnDate     = now,
                        UpdatedOnDate     = now
                    });
                }
            }

            // Cập nhật mapping bàn và mặt sàn nếu DineIn và có TableIds thay đổi
            if (order.ServiceType == ServiceTypeConstants.DineIn && request.TableIds != null && request.TableIds.Any())
            {
                var existingTables = await orderTableRepo.Get(x => x.OrderId == orderId && x.BranchCode == request.BranchCode).ToListAsync();
                foreach (var et in existingTables)
                {
                    orderTableRepo.Delete(et);
                }

                Guid floorId = Guid.Empty;
                string? floorName = null;
                var tableRepo = _unit.GetRepository<DiningTable>();

                foreach (var tableId in request.TableIds)
                {
                    var table = await tableRepo.Get(x => x.Id == tableId && x.Status != 0).FirstOrDefaultAsync();
                    if (table == null) continue;

                    if (floorId == Guid.Empty)
                    {
                        floorId = table.FloorPlanId;
                        var floor = await _unit.GetRepository<FloorPlan>().Get(x => x.Id == floorId).FirstOrDefaultAsync();
                        floorName = floor?.Name;
                    }

                    orderTableRepo.Add(new SalesOrderTable
                    {
                        Id = Guid.NewGuid(),
                        BranchCode = request.BranchCode,
                        OrderId = orderId,
                        TableId = tableId,
                        TableCode = table.Code,
                        TableName = table.Name,
                        FloorId = table.FloorPlanId,
                        CreatedOnDate = now
                    });
                }

                if (floorId != Guid.Empty)
                {
                    order.FloorId = floorId;
                    order.FloorName = floorName;
                }
            }

            // Cập nhật tổng tiền
            order.SubTotal       = subTotal;
            order.DiscountAmount = discountTotal;
            order.VatAmount      = vatTotal;
            order.TotalAmount    = Math.Max(0, subTotal - discountTotal + vatTotal);
            if (order.TotalAmount == 0)
            {
                order.VatAmount = 0;
            }

            if (isModified && (order.Status == OrderStatusConstants.WaitingPayment || order.Status == OrderStatusConstants.Checked))
            {
                var oldStatus = order.Status;
                order.Status = OrderStatusConstants.Serving;
                await _logger.InfoAsync($"Order status changed: OrderId={orderId}, BranchCode={request.BranchCode}, Action=UpdateModify, OldStatus={oldStatus}, NewStatus={OrderStatusConstants.Serving}");
            }

            orderRepo.Update(order);
            await _unit.SaveAsync();

            // Recalculate totals including promotion (BE-07)
            await CalculateOrderTotal(orderId, request.BranchCode);

            await BroadcastSalesEventAsync(request.BranchCode, new SalesRealtimeEventDto
            {
                EventType = "OrderUpdated",
                OrderId = orderId,
                OrderCode = order.OrderCode,
                NewStatus = order.Status,
                ServiceType = order.ServiceType
            });

            if (cancelledSentItem)
                _ = _mqttSpeakerService.NotifyKitchenCancellationAsync(order.OrderCode);

            return new ResponseObject<OrderActionResultDto>
            {
                Code    = Code.Success,
                Message = "Cập nhật order thành công!",
                Data    = new OrderActionResultDto
                {
                    OrderId   = orderId,
                    OrderCode = order.OrderCode,
                    Status    = order.Status,
                    Message   = "Order đã được cập nhật.",
                    Success   = true
                }
            };
        }

        // ═══════════════════════════════════════════════════════
        // OPEN DINE-IN TABLE
        // ═══════════════════════════════════════════════════════

        public async Task<Response> OpenDineInTable(DineInOpenTableRequest request, string? userCode = null, string? userName = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.BranchCode))
                    return new Response(Code.BadRequest, "Mã chi nhánh không được để trống!");

                bool isTakeAwayOrDelivery = request.ServiceType == ServiceTypeConstants.TakeAway || request.ServiceType == ServiceTypeConstants.Delivery;
                bool hasTables = !isTakeAwayOrDelivery && request.TableIds != null && request.TableIds.Any();
                request.TableIds = request.TableIds ?? new List<Guid>();

                var branchRepo = _unit.GetRepository<Branchs>();
                var branch = await branchRepo.Get(x => x.Code == request.BranchCode && x.Status != 0).FirstOrDefaultAsync();
                if (branch == null)
                    return new Response(Code.NotFound, "Chi nhánh không tồn tại hoặc đã bị khóa!");

                var tableRepo = _unit.GetRepository<DiningTable>();
                var orderRepo = _unit.GetRepository<SalesOrder>();
                var orderTableRepo = _unit.GetRepository<SalesOrderTable>();

                // Validate tables belong to branch
                if (hasTables)
                {
                    foreach (var tId in request.TableIds)
                    {
                        if (!await ValidateTableBranch(tId, request.BranchCode))
                            return new Response(Code.BadRequest, $"Bàn '{tId}' không thuộc chi nhánh này!");
                    }
                }

                // ── Validate Capacity ────────────────────────────
                if (hasTables)
                {
                    var capRes = await ValidateTableCapacityForOrder(request.BranchCode, request.TableIds, request.CustomerCount);
                    if (capRes.Code != Code.Success)
                    {
                        return capRes;
                    }
                }

                // Check conflict
                if (hasTables && !request.Force)
                {
                    var tableIds = request.TableIds;
                    var activeOrderTable = await orderTableRepo.Get(x => tableIds.Contains(x.TableId) && x.BranchCode == request.BranchCode)
                        .Join(orderRepo.Get(o => o.BranchCode == request.BranchCode && !OrderStatusConstants.InactiveStatuses.Contains(o.Status)),
                              ot => ot.OrderId,
                              o => o.Id,
                              (ot, o) => ot)
                        .FirstOrDefaultAsync();

                    if (activeOrderTable != null)
                    {
                        return new Response((Code)409, "Bàn đang có khách, vui lòng xác nhận mở thêm order mới");
                    }
                }

                var now = DateTime.Now;
                var openTime = request.OpenTime ?? request.OrderDate ?? now;
                if (openTime.Kind == DateTimeKind.Utc) openTime = openTime.ToLocalTime();
                var orderId = Guid.NewGuid();
                var orderCode = await GenerateOrderCode(request.BranchCode, openTime);

                // Floor info
                Guid floorId = Guid.Empty;
                string? floorName = null;
                if (hasTables)
                {
                    var firstTable = await tableRepo.Get(x => x.Id == request.TableIds.First()).FirstOrDefaultAsync();
                    if (firstTable != null)
                    {
                        floorId = firstTable.FloorPlanId;
                        var floor = await _unit.GetRepository<FloorPlan>().Get(x => x.Id == floorId).FirstOrDefaultAsync();
                        floorName = floor?.Name;
                    }
                }
                if (floorId == Guid.Empty)
                {
                    var defaultFloor = await _unit.GetRepository<FloorPlan>().Get(x => x.BranchId == branch.Id && x.Status != 0).OrderBy(x => x.Code).FirstOrDefaultAsync();
                    if (defaultFloor != null)
                    {
                        floorId = defaultFloor.Id;
                        floorName = defaultFloor.Name;
                    }
                }

                using var trans = await _unit.BeginTransactionAsync();

                var salesOrder = new SalesOrder
                {
                    Id = orderId,
                    BranchCode = request.BranchCode,
                    OrderCode = orderCode,
                    OrderDate = openTime.Date,
                    OpenTime = openTime,
                    WaiterId = request.WaiterId,
                    WaiterName = request.WaiterName,
                    FloorId = floorId,
                    FloorName = floorName,
                    CustomerCount = request.CustomerCount > 0 ? request.CustomerCount : 1,
                    ServiceType = isTakeAwayOrDelivery ? request.ServiceType! : ServiceTypeConstants.DineIn,
                    Status = OrderStatusConstants.Serving,
                    CreatedOnDate = now,
                    CreatedByUserCode = !string.IsNullOrEmpty(userCode) ? userCode : "system",
                    CreatedByUserName = !string.IsNullOrEmpty(userName) ? userName : "System",
                    UpdatedOnDate = now,
                    UpdatedByUserCode = userCode,
                    UpdatedByUserName = userName
                };
                orderRepo.Add(salesOrder);

                if (hasTables)
                {
                    foreach (var tableId in request.TableIds)
                    {
                        var table = await tableRepo.Get(x => x.Id == tableId).FirstOrDefaultAsync();
                        if (table == null) continue;

                        orderTableRepo.Add(new SalesOrderTable
                        {
                            Id = Guid.NewGuid(),
                            BranchCode = request.BranchCode,
                            OrderId = orderId,
                            TableId = tableId,
                            TableCode = table.Code,
                            TableName = table.Name,
                            FloorId = table.FloorPlanId,
                            CreatedOnDate = now
                        });
                    }
                }

                await _unit.SaveAsync();
                await trans.CommitAsync();

                await BroadcastSalesEventAsync(request.BranchCode, new SalesRealtimeEventDto
                {
                    EventType = "TableOpened",
                    OrderId = orderId,
                    OrderCode = orderCode,
                    NewStatus = OrderStatusConstants.Serving,
                    ServiceType = isTakeAwayOrDelivery ? request.ServiceType! : "DineIn"
                });

                return new ResponseObject<OrderActionResultDto>
                {
                    Code = Code.Success,
                    Message = "Mở bàn thành công!",
                    Data = new OrderActionResultDto
                    {
                        OrderId = orderId,
                        OrderCode = orderCode,
                        Status = OrderStatusConstants.Serving,
                        Success = true,
                        Message = isTakeAwayOrDelivery ? "Đơn hàng đã được tạo thành công." : "Bàn đã được mở ở trạng thái đang phục vụ."
                    }
                };
            }
            catch (Exception ex)
            {
                var detailMessage = ex.InnerException != null ? $"{ex.Message} -> {ex.InnerException.Message}" : ex.Message;
                await _logger.ErrorAsync("SalesOrderService.OpenDineInTable", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi: " + detailMessage);
            }
        }

        // ═══════════════════════════════════════════════════════
        // RESERVE ORDER
        // ═══════════════════════════════════════════════════════

        public async Task<Response> ReserveOrder(DineInOpenTableRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.BranchCode))
                    return new Response(Code.BadRequest, "Mã chi nhánh không được để trống!");

                if (!OrderStatusConstants.CanTransition(null, OrderStatusConstants.Reserved, "Reserve"))
                {
                    return new Response(Code.BadRequest, "Không thể chuyển trạng thái sang Đặt trước!");
                }

                bool hasTables = request.TableIds != null && request.TableIds.Any();
                request.TableIds = request.TableIds ?? new List<Guid>();

                var branchRepo = _unit.GetRepository<Branchs>();
                var branch = await branchRepo.Get(x => x.Code == request.BranchCode && x.Status != 0).FirstOrDefaultAsync();
                if (branch == null)
                    return new Response(Code.NotFound, "Chi nhánh không tồn tại hoặc đã bị khóa!");

                var tableRepo = _unit.GetRepository<DiningTable>();
                var orderRepo = _unit.GetRepository<SalesOrder>();
                var orderTableRepo = _unit.GetRepository<SalesOrderTable>();

                // Validate tables belong to branch
                if (hasTables)
                {
                    foreach (var tId in request.TableIds)
                    {
                        if (!await ValidateTableBranch(tId, request.BranchCode))
                            return new Response(Code.BadRequest, $"Bàn '{tId}' không thuộc chi nhánh này!");
                    }
                }

                // ── Validate Capacity ────────────────────────────
                if (hasTables)
                {
                    var capRes = await ValidateTableCapacityForOrder(request.BranchCode, request.TableIds, request.CustomerCount);
                    if (capRes.Code != Code.Success)
                    {
                        return capRes;
                    }
                }

                // Check conflict
                if (hasTables && !request.Force)
                {
                    var tableIds = request.TableIds;
                    var activeOrderTable = await orderTableRepo.Get(x => tableIds.Contains(x.TableId) && x.BranchCode == request.BranchCode)
                        .Join(orderRepo.Get(o => o.BranchCode == request.BranchCode && !OrderStatusConstants.InactiveStatuses.Contains(o.Status)),
                              ot => ot.OrderId,
                              o => o.Id,
                              (ot, o) => ot)
                        .FirstOrDefaultAsync();

                    if (activeOrderTable != null)
                    {
                        return new Response((Code)409, "Bàn đang có khách, vui lòng xác nhận mở thêm order mới");
                    }
                }

                var now = DateTime.Now;
                var openTime = request.OpenTime ?? request.OrderDate ?? now;
                if (openTime.Kind == DateTimeKind.Utc) openTime = openTime.ToLocalTime();
                var orderId = Guid.NewGuid();
                var orderCode = await GenerateOrderCode(request.BranchCode, openTime);

                // Floor info
                Guid floorId = Guid.Empty;
                string? floorName = null;
                if (hasTables)
                {
                    var firstTable = await tableRepo.Get(x => x.Id == request.TableIds.First()).FirstOrDefaultAsync();
                    if (firstTable != null)
                    {
                        floorId = firstTable.FloorPlanId;
                        var floor = await _unit.GetRepository<FloorPlan>().Get(x => x.Id == floorId).FirstOrDefaultAsync();
                        floorName = floor?.Name;
                    }
                }

                using var trans = await _unit.BeginTransactionAsync();

                var salesOrder = new SalesOrder
                {
                    Id = orderId,
                    BranchCode = request.BranchCode,
                    OrderCode = orderCode,
                    OrderDate = openTime.Date,
                    OpenTime = openTime,
                    WaiterId = request.WaiterId,
                    WaiterName = request.WaiterName,
                    FloorId = floorId,
                    FloorName = floorName,
                    CustomerCount = request.CustomerCount > 0 ? request.CustomerCount : 1,
                    ServiceType = ServiceTypeConstants.DineIn,
                    Status = OrderStatusConstants.Reserved,
                    CreatedOnDate = now,
                    CreatedByUserCode = "system",
                    CreatedByUserName = "System"
                };
                orderRepo.Add(salesOrder);

                foreach (var tableId in request.TableIds)
                {
                    var table = await tableRepo.Get(x => x.Id == tableId).FirstOrDefaultAsync();
                    if (table == null) continue;

                    orderTableRepo.Add(new SalesOrderTable
                    {
                        Id = Guid.NewGuid(),
                        BranchCode = request.BranchCode,
                        OrderId = orderId,
                        TableId = tableId,
                        TableCode = table.Code,
                        TableName = table.Name,
                        FloorId = table.FloorPlanId,
                        CreatedOnDate = now
                    });
                }

                await _unit.SaveAsync();
                await trans.CommitAsync();

                await _logger.InfoAsync($"Order status changed: OrderId={orderId}, BranchCode={request.BranchCode}, Action=Reserve, OldStatus=Draft, NewStatus={OrderStatusConstants.Reserved}");

                await BroadcastSalesEventAsync(request.BranchCode, new SalesRealtimeEventDto
                {
                    EventType = "TableOpened",
                    OrderId = orderId,
                    OrderCode = orderCode,
                    NewStatus = OrderStatusConstants.Reserved,
                    ServiceType = "DineIn"
                });

                return new ResponseObject<OrderActionResultDto>
                {
                    Code = Code.Success,
                    Message = "Đặt trước bàn thành công!",
                    Data = new OrderActionResultDto
                    {
                        OrderId = orderId,
                        OrderCode = orderCode,
                        Status = OrderStatusConstants.Reserved,
                        Success = true,
                        Message = "Bàn đã được đặt trước."
                    }
                };
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("SalesOrderService.ReserveOrder", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi: " + ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════
        // CHECK IN / OPEN RESERVED TABLE
        // ═══════════════════════════════════════════════════════

        public async Task<Response> CheckInOrder(Guid orderId, string branchCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(branchCode))
                    return new Response(Code.BadRequest, "Mã chi nhánh không được để trống!");

                if (orderId == Guid.Empty)
                    return new Response(Code.BadRequest, "OrderId không hợp lệ!");

                var orderRepo = _unit.GetRepository<SalesOrder>();
                var order = await orderRepo.Get(x => x.Id == orderId && x.BranchCode == branchCode).FirstOrDefaultAsync();
                if (order == null)
                    return new Response(Code.NotFound, "Đơn hàng không tồn tại hoặc không thuộc chi nhánh này!");

                if (order.Status != OrderStatusConstants.Reserved)
                {
                    return new Response(Code.BadRequest, $"Đơn hàng đang ở trạng thái '{order.Status}', không thể mở bàn đặt trước!");
                }

                if (!OrderStatusConstants.CanTransition(order.Status, OrderStatusConstants.Serving, "CheckIn"))
                {
                    return new Response(Code.BadRequest, "Không thể chuyển trạng thái sang Đang phục vụ!");
                }

                var now = DateTime.Now;
                var oldStatus = order.Status;

                using var trans = await _unit.BeginTransactionAsync();

                order.Status = OrderStatusConstants.Serving;
                order.UpdatedOnDate = now;
                orderRepo.Update(order);

                bool requireKitchen = await IsRequireKitchenSendEnabled();
                bool trackKitchenSend = requireKitchen || await IsKitchenPrintEnabled();
                var itemRepo = _unit.GetRepository<SalesOrderItem>();
                var items = await itemRepo.Get(x => x.OrderId == orderId && x.BranchCode == branchCode && x.IsCancelled == 0).ToListAsync();

                if (!requireKitchen && items.Any())
                {
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
                }

                await _unit.SaveAsync();
                await trans.CommitAsync();

                await _logger.InfoAsync($"Order status changed: OrderId={orderId}, BranchCode={branchCode}, Action=CheckIn, OldStatus={oldStatus}, NewStatus={OrderStatusConstants.Serving}");

                await BroadcastSalesEventAsync(branchCode, new SalesRealtimeEventDto
                {
                    EventType = "TableOpened",
                    OrderId = orderId,
                    OrderCode = order.OrderCode,
                    NewStatus = OrderStatusConstants.Serving,
                    ServiceType = order.ServiceType
                });

                return new ResponseObject<OrderActionResultDto>
                {
                    Code = Code.Success,
                    Message = "Mở bàn thành công!",
                    Data = new OrderActionResultDto
                    {
                        OrderId = orderId,
                        OrderCode = order.OrderCode,
                        Status = OrderStatusConstants.Serving,
                        Success = true,
                        Message = "Bàn đặt trước đã được mở sang trạng thái đang phục vụ."
                    }
                };
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("SalesOrderService.CheckInOrder", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi: " + ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════
        // CREATE TAKEAWAY ORDER
        // ═══════════════════════════════════════════════════════

        public async Task<Response> CreateTakeAwayOrder(CreateOrUpdateOrderRequest request)
        {
            try
            {
                request.ServiceType = ServiceTypeConstants.TakeAway;
                request.TableIds = new List<Guid>(); // no tables for takeaway
                var res = await SaveOrder(request);
                return res;
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("SalesOrderService.CreateTakeAwayOrder", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi: " + ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════
        // CREATE DELIVERY ORDER
        // ═══════════════════════════════════════════════════════

        public async Task<Response> CreateDeliveryOrder(CreateOrUpdateOrderRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                    return new Response(Code.BadRequest, "Số điện thoại là bắt buộc đối với đơn giao hàng!");

                if (string.IsNullOrWhiteSpace(request.DeliveryAddress))
                    return new Response(Code.BadRequest, "Địa chỉ giao hàng là bắt buộc đối với đơn giao hàng!");

                request.ServiceType = ServiceTypeConstants.Delivery;
                request.TableIds = new List<Guid>(); // no tables for delivery
                var res = await SaveOrder(request);
                return res;
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("SalesOrderService.CreateDeliveryOrder", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi: " + ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════
        // SEND ORDER TO KITCHEN BAR
        // ═══════════════════════════════════════════════════════

        public async Task<Response> SendOrderToKitchenBar(Guid orderId, string branchCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(branchCode))
                    return new Response(Code.BadRequest, "Mã chi nhánh không được để trống!");

                if (orderId == Guid.Empty)
                    return new Response(Code.BadRequest, "OrderId không hợp lệ!");

                var orderRepo = _unit.GetRepository<SalesOrder>();
                var order = await orderRepo.Get(x => x.Id == orderId && x.BranchCode == branchCode).FirstOrDefaultAsync();
                if (order == null)
                    return new Response(Code.NotFound, "Order không tồn tại!");

                var oldStatus = order.Status;
                if (!OrderStatusConstants.CanTransition(oldStatus, OrderStatusConstants.Serving, "SendKitchenBar"))
                {
                    return new Response(Code.BadRequest, $"Không thể gửi bếp/bar cho order ở trạng thái '{oldStatus}'!");
                }

                // QR orders created before CustomerId was persisted still carry the validated customer code.
                if (order.IncludesQrOrder && !order.CustomerId.HasValue &&
                    !string.IsNullOrWhiteSpace(order.CustomerCode) && order.CustomerCode != "GUEST")
                {
                    var customer = await _unit.GetRepository<BusinessPartner>()
                        .Get(x => x.Code == order.CustomerCode && x.Status != 0 && (x.PartnerTypes == 1 || x.PartnerTypes == 3))
                        .FirstOrDefaultAsync();
                    if (customer != null)
                    {
                        order.CustomerId = customer.Id;
                        orderRepo.Update(order);
                        await _unit.SaveAsync();
                    }
                }

                bool requireKitchen = await IsRequireKitchenSendEnabled();
                if (!requireKitchen)
                {
                    // Khi tắt setting bắt buộc gửi bếp: chỉ duyệt/lưu đơn sang trạng thái hoạt động (Serving), đánh dấu món đã hoàn tất phục vụ, KHÔNG gửi lệnh sang Bếp/Bar
                    using var trans = await _unit.BeginTransactionAsync();
                    var itemRepo = _unit.GetRepository<SalesOrderItem>();
                    var items = await itemRepo.Get(x => x.OrderId == orderId && x.BranchCode == branchCode && x.IsCancelled == 0).ToListAsync();

                    var now = DateTime.Now;
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

                    if (order.Status == OrderStatusConstants.WaitingConfirm ||
                        order.Status == OrderStatusConstants.WaitingService ||
                        string.Equals(order.Status, "Draft", StringComparison.OrdinalIgnoreCase) ||
                        order.Status == OrderStatusConstants.Reserved)
                    {
                        order.Status = OrderStatusConstants.Serving;
                        order.UpdatedOnDate = now;
                        orderRepo.Update(order);
                    }

                    await _unit.SaveAsync();
                    await trans.CommitAsync();

                    await BroadcastSalesEventAsync(branchCode, new SalesRealtimeEventDto
                    {
                        EventType = "OrderConfirmed",
                        OrderId = orderId,
                        OrderCode = order.OrderCode,
                        NewStatus = order.Status,
                        ServiceType = order.ServiceType
                    });

                    return new ResponseObject<OrderActionResultDto>
                    {
                        Code = Code.Success,
                        Message = "Đã duyệt và lưu đơn thành công!",
                        Data = new OrderActionResultDto
                        {
                            OrderId = orderId,
                            OrderCode = order.OrderCode,
                            Status = order.Status,
                            Success = true,
                            Message = "Đã duyệt đơn và lưu vào bàn."
                        }
                    };
                }

                var sendReq = new AnFood.Application.Bussiness.KitchenBussiness.SendKitchenRequestDto
                {
                    BranchCode = branchCode,
                    OrderId = orderId,
                    SendBy = "Thu ngân"
                };

                var kitchenRes = await _kitchenHandler.SendKitchen(orderId, sendReq);
                if (kitchenRes.Code != Code.Success)
                {
                    return kitchenRes;
                }

                // Re-fetch order status after kitchen send
                var updatedOrder = await orderRepo.Get(x => x.Id == orderId).FirstOrDefaultAsync();

                await BroadcastSalesEventAsync(branchCode, new SalesRealtimeEventDto
                {
                    EventType = "OrderConfirmed",
                    OrderId = orderId,
                    OrderCode = updatedOrder?.OrderCode ?? order.OrderCode,
                    NewStatus = updatedOrder?.Status ?? order.Status,
                    ServiceType = order.ServiceType
                });

                return new ResponseObject<OrderActionResultDto>
                {
                    Code = Code.Success,
                    Message = "Gửi bếp/bar thành công!",
                    Data = new OrderActionResultDto
                    {
                        OrderId = orderId,
                        OrderCode = updatedOrder?.OrderCode ?? order.OrderCode,
                        Status = updatedOrder?.Status ?? order.Status,
                        Success = true,
                        Message = "Đã báo chế biến cho các món mới."
                    }
                };
            }
            catch (Exception ex)
            {
                var detailMessage = ex.InnerException != null ? $"{ex.Message} -> {ex.InnerException.Message}" : ex.Message;
                await _logger.ErrorAsync("SalesOrderService.SendOrderToKitchenBar", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi: " + detailMessage);
            }
        }

        // ═══════════════════════════════════════════════════════
        // TRANSFER TABLE
        // ═══════════════════════════════════════════════════════

        public async Task<Response> TransferTable(Guid orderId, List<Guid> targetTableIds, string branchCode, string? note = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(branchCode))
                    return new Response(Code.BadRequest, "Mã chi nhánh không được để trống!");

                if (orderId == Guid.Empty)
                    return new Response(Code.BadRequest, "OrderId không hợp lệ!");

                if (targetTableIds == null || !targetTableIds.Any())
                    return new Response(Code.BadRequest, "Chưa chọn bàn đích chuyển đến!");

                var orderRepo = _unit.GetRepository<SalesOrder>();
                var order = await orderRepo.Get(x => x.Id == orderId && x.BranchCode == branchCode).FirstOrDefaultAsync();
                if (order == null)
                    return new Response(Code.NotFound, "Order không tồn tại!");

                if (!OrderStatusConstants.IsActive(order.Status))
                    return new Response(Code.BadRequest, "Không thể chuyển bàn của hóa đơn đã hoàn thành hoặc hủy!");

                var tableRepo = _unit.GetRepository<DiningTable>();
                var orderTableRepo = _unit.GetRepository<SalesOrderTable>();

                // Validate targets
                foreach (var tId in targetTableIds)
                {
                    if (!await ValidateTableBranch(tId, branchCode))
                        return new Response(Code.BadRequest, $"Bàn đích '{tId}' không thuộc chi nhánh này!");
                }

                // ── Validate Capacity ────────────────────────────
                var capRes = await ValidateTableCapacityForOrder(branchCode, targetTableIds, order.CustomerCount, new List<Guid> { orderId });
                if (capRes.Code != Code.Success)
                {
                    return capRes;
                }

                using var trans = await _unit.BeginTransactionAsync();

                // Delete old mappings
                var oldMappings = await orderTableRepo.Get(x => x.OrderId == orderId && x.BranchCode == branchCode).ToListAsync();
                foreach (var m in oldMappings)
                {
                    orderTableRepo.Delete(m);
                }

                // Add new mappings
                Guid floorId = Guid.Empty;
                string? floorName = null;
                var now = DateTime.Now;
                var targetTableNamesList = new List<string>();

                foreach (var tId in targetTableIds)
                {
                    var table = await tableRepo.Get(x => x.Id == tId).FirstOrDefaultAsync();
                    if (table == null) continue;

                    if (floorId == Guid.Empty)
                    {
                        floorId = table.FloorPlanId;
                        var floor = await _unit.GetRepository<FloorPlan>().Get(x => x.Id == floorId).FirstOrDefaultAsync();
                        floorName = floor?.Name;
                    }

                    targetTableNamesList.Add(table.Name);

                    orderTableRepo.Add(new SalesOrderTable
                    {
                        Id = Guid.NewGuid(),
                        BranchCode = branchCode,
                        OrderId = orderId,
                        TableId = tId,
                        TableCode = table.Code,
                        TableName = table.Name,
                        FloorId = table.FloorPlanId,
                        CreatedOnDate = now
                    });
                }

                // Cập nhật tên bàn trên các bill bếp (KitchenRequest) chưa hoàn thành
                var kitchenRequestRepo = _unit.GetRepository<KitchenRequest>();
                var activeKitchenRequests = await kitchenRequestRepo
                    .Get(kr => kr.OrderId == orderId && kr.Status != "Completed" && kr.Status != "Cancelled")
                    .ToListAsync();

                if (activeKitchenRequests.Any())
                {
                    string combinedTableName = targetTableNamesList.Any() ? string.Join(", ", targetTableNamesList) : "Bàn";
                    foreach (var kr in activeKitchenRequests)
                    {
                        kr.TableId = targetTableIds.FirstOrDefault();
                        kr.TableName = combinedTableName;
                        kitchenRequestRepo.Update(kr);
                    }
                }

                // Update order floor
                order.FloorId = floorId;
                order.FloorName = floorName;

                var oldTableCode = string.Join(", ", oldMappings.Select(x => !string.IsNullOrEmpty(x.TableCode) ? x.TableCode : x.TableName));
                var autoNote = $"Chuyển từ bàn {oldTableCode} {order.OrderCode}".Trim();
                var transferNoteToSave = string.IsNullOrWhiteSpace(note) ? autoNote : note;

                var existingNote = order.Note ?? "";
                var noteParts = existingNote.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(p => !p.StartsWith("Chuyển từ bàn") && !p.StartsWith("Chuyển từ"))
                    .ToList();

                var baseNote = string.Join(" | ", noteParts);
                var updatedNote = string.IsNullOrWhiteSpace(baseNote)
                    ? transferNoteToSave
                    : $"{baseNote} | {transferNoteToSave}";

                if (updatedNote.Length > 1000)
                {
                    updatedNote = updatedNote.Substring(0, 1000);
                }
                order.Note = updatedNote;

                order.UpdatedOnDate = now;
                orderRepo.Update(order);

                await _unit.SaveAsync();
                await trans.CommitAsync();

                await BroadcastSalesEventAsync(branchCode, new SalesRealtimeEventDto
                {
                    EventType = "TableTransferred",
                    OrderId = order.Id,
                    OrderCode = order.OrderCode,
                    NewStatus = order.Status,
                    ServiceType = order.ServiceType
                });

                try
                {
                    await _kitchenHubSender.SendRequestsUpdatedAsync(branchCode);
                }
                catch (Exception kitchenEx)
                {
                    await _logger.ErrorAsync($"TransferTable SendRequestsUpdatedAsync error: {kitchenEx.Message}", kitchenEx);
                }

                return new Response(Code.Success, "Chuyển bàn thành công!");
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("SalesOrderService.TransferTable", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi: " + ex.Message);
            }
        }


        // ═══════════════════════════════════════════════════════
        // MERGE ORDERS
        // ═══════════════════════════════════════════════════════

        public async Task<Response> MergeOrders(Guid targetOrderId, List<Guid> sourceOrderIds, List<Guid>? tableIds, string branchCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(branchCode))
                    return new Response(Code.BadRequest, "Mã chi nhánh không được để trống!");

                if (targetOrderId == Guid.Empty || sourceOrderIds == null || !sourceOrderIds.Any())
                    return new Response(Code.BadRequest, "Thông tin gộp không hợp lệ!");

                var orderRepo = _unit.GetRepository<SalesOrder>();
                var targetOrder = await orderRepo.Get(x => x.Id == targetOrderId && x.BranchCode == branchCode).FirstOrDefaultAsync();
                if (targetOrder == null)
                    return new Response(Code.NotFound, "Hóa đơn đích không tồn tại!");

                if (!OrderStatusConstants.IsActive(targetOrder.Status))
                    return new Response(Code.BadRequest, "Hóa đơn đích đã kết thúc!");

                var sourceOrders = new List<SalesOrder>();
                foreach (var srcId in sourceOrderIds)
                {
                    if (srcId == targetOrderId) continue;
                    var srcOrder = await orderRepo.Get(x => x.Id == srcId && x.BranchCode == branchCode).FirstOrDefaultAsync();
                    if (srcOrder == null) continue;
                    if (!OrderStatusConstants.IsActive(srcOrder.Status))
                        return new Response(Code.BadRequest, $"Hóa đơn '{srcOrder.OrderCode}' đã kết thúc!");
                    sourceOrders.Add(srcOrder);
                }

                var orderTableRepo = _unit.GetRepository<SalesOrderTable>();
                var allOrderIds = new List<Guid> { targetOrderId };
                allOrderIds.AddRange(sourceOrders.Select(x => x.Id));

                var allTableIds = await orderTableRepo.Get(x => allOrderIds.Contains(x.OrderId) && x.BranchCode == branchCode)
                    .Select(x => x.TableId)
                    .Distinct()
                    .ToListAsync();

                var totalConsolidatedCustomers = targetOrder.CustomerCount + sourceOrders.Sum(x => x.CustomerCount);

                if (allTableIds != null && allTableIds.Any())
                {
                    var capRes = await ValidateTableCapacityForOrder(branchCode, allTableIds, totalConsolidatedCustomers, allOrderIds);
                    if (capRes.Code != Code.Success)
                    {
                        return capRes;
                    }
                }

                using var trans = await _unit.BeginTransactionAsync();
                targetOrder.IncludesQrOrder = targetOrder.IncludesQrOrder || sourceOrders.Any(x => x.IncludesQrOrder);

                var itemRepo = _unit.GetRepository<SalesOrderItem>();
                var kitchenRequestRepo = _unit.GetRepository<KitchenRequest>();
                var kitchenRequestDetailRepo = _unit.GetRepository<KitchenRequestDetail>();

                var mergedRequestIds = await kitchenRequestRepo.Get(r => allOrderIds.Contains(r.OrderId) && r.BranchCode == branchCode)
                    .Select(r => r.Id)
                    .ToListAsync();

                var targetItems = await itemRepo.Get(x => x.OrderId == targetOrderId && x.BranchCode == branchCode).ToListAsync();
                var now = DateTime.Now;

                // Merge status analyzer
                var statuses = new List<string> { targetOrder.Status };

                foreach (var srcId in sourceOrderIds)
                {
                    if (srcId == targetOrderId) continue;

                    var srcOrder = sourceOrders.FirstOrDefault(x => x.Id == srcId);
                    if (srcOrder == null) continue;

                    statuses.Add(srcOrder.Status);

                    // Add customer count
                    targetOrder.CustomerCount += srcOrder.CustomerCount;

                    // Move tables mapping to targetOrderId (if tableIds is not specified)
                    if (tableIds == null || !tableIds.Any())
                    {
                        var srcTables = await orderTableRepo.Get(x => x.OrderId == srcId && x.BranchCode == branchCode).ToListAsync();
                        foreach (var tbl in srcTables)
                        {
                            // Check if already mapped
                            var alreadyMapped = await orderTableRepo.Get(x => x.OrderId == targetOrderId && x.TableId == tbl.TableId && x.BranchCode == branchCode).AnyAsync();
                            if (!alreadyMapped)
                            {
                                tbl.OrderId = targetOrderId;
                                orderTableRepo.Update(tbl);
                            }
                            else
                            {
                                orderTableRepo.Delete(tbl);
                            }
                        }
                    }
                    else
                    {
                        // Delete table mappings of source order since we are replacing them explicitly
                        var srcTables = await orderTableRepo.Get(x => x.OrderId == srcId && x.BranchCode == branchCode).ToListAsync();
                        foreach (var tbl in srcTables)
                        {
                            orderTableRepo.Delete(tbl);
                        }
                    }

                    // Merge items
                    var srcItems = await itemRepo.Get(x => x.OrderId == srcId && x.BranchCode == branchCode).ToListAsync();
                    foreach (var sItem in srcItems)
                    {
                        if (sItem.IsCancelled == 1) continue;

                        var isGift = !string.IsNullOrEmpty(sItem.PromotionCode) && sItem.DiscountRate == 100;
                        var match = isGift ? null : targetItems.FirstOrDefault(x => x.ItemId == sItem.ItemId
                                                                 && x.SelectedOptionsJson == sItem.SelectedOptionsJson
                                                                 && x.UnitPrice == sItem.UnitPrice
                                                                 && x.IsCancelled == 0
                                                                 && (string.IsNullOrEmpty(x.PromotionCode) || x.DiscountRate != 100));

                        if (match != null)
                        {
                            match.Quantity += sItem.Quantity;
                            match.ServedQty += sItem.ServedQty;
                            match.CancelledQuantity += sItem.CancelledQuantity;
                            match.SoldOutQty += sItem.SoldOutQty;
                            match.SentQty += sItem.SentQty;
                            match.RemainingQty = match.Quantity - match.ServedQty - match.SoldOutQty;
                            if (match.RemainingQty < 0) match.RemainingQty = 0;
                            match.IsSentToKitchen = (match.IsSentToKitchen == 1 || sItem.IsSentToKitchen == 1) ? 1 : 0;
                            match.KitchenStatus = DetermineKitchenStatus(match.Quantity, match.ServedQty, match.SoldOutQty, match.CancelledQuantity, match.IsSentToKitchen, match.SentQty);

                            if (!string.IsNullOrWhiteSpace(sItem.Note))
                            {
                                if (string.IsNullOrWhiteSpace(match.Note))
                                    match.Note = sItem.Note;
                                else if (match.Note != sItem.Note && !match.Note.Contains(sItem.Note))
                                    match.Note = $"{match.Note}, {sItem.Note}";
                            }

                            match.Amount = Math.Round((decimal)match.Quantity * match.UnitPrice, 0, MidpointRounding.AwayFromZero);

                            var mergeMiRepo = _unit.GetRepository<MenuItem>();
                            var mergeMi = await mergeMiRepo.Get(x => x.Id == match.ItemId).FirstOrDefaultAsync();
                            var effQty2625 = (decimal)(match.Quantity > 0 ? match.Quantity : 1);
                            var unitVat2625 = CalculateUnitVat(match.UnitPrice, match.VatRate, mergeMi, match.SelectedOptionsJson);
                            var unitGross2625 = match.UnitPrice + unitVat2625;
                            // Fix rounding: tính discount tại cấp unit trước từ giá sau thuế (unitGross)
                            var unitDisc2610 = match.DiscountRate > 0 ? Math.Round(unitGross2625 * (decimal)(match.DiscountRate / 100), 0, MidpointRounding.AwayFromZero) : 0m;
                            match.DiscountAmount = unitDisc2610 * (decimal)match.Quantity;

                            var unitDisc2625 = Math.Round(match.DiscountAmount / effQty2625, 0, MidpointRounding.AwayFromZero);
                            var unitTotal2625 = Math.Max(0, unitGross2625 - unitDisc2625);
                            match.VatAmount = Math.Round(unitVat2625 * (decimal)match.Quantity, 0, MidpointRounding.AwayFromZero);
                            match.TotalAmount = Math.Round(unitTotal2625 * (decimal)match.Quantity, 0, MidpointRounding.AwayFromZero);
                            match.UpdatedOnDate = now;
                            itemRepo.Update(match);

                            // Update kitchen request details to match.Id
                            var detailsToUpdate = await kitchenRequestDetailRepo.Get(x => x.OrderDetailId == sItem.Id).ToListAsync();
                            foreach (var det in detailsToUpdate)
                            {
                                det.OrderDetailId = match.Id;
                                kitchenRequestDetailRepo.Update(det);
                            }
                        }
                        else
                        {
                            // Add new line
                            var lineNo = targetItems.Any() ? targetItems.Max(x => x.LineNo) + 1 : 1;
                            var newItem = new SalesOrderItem
                            {
                                Id = Guid.NewGuid(),
                                BranchCode = branchCode,
                                OrderId = targetOrderId,
                                LineNo = lineNo,
                                ItemId = sItem.ItemId,
                                ItemCode = sItem.ItemCode,
                                ItemName = sItem.ItemName,
                                UnitName = sItem.UnitName,
                                Quantity = sItem.Quantity,
                                ServedQty = sItem.ServedQty,
                                CancelledQuantity = sItem.CancelledQuantity,
                                SoldOutQty = sItem.SoldOutQty,
                                SentQty = sItem.SentQty,
                                RemainingQty = sItem.RemainingQty,
                                KitchenStatus = sItem.KitchenStatus,
                                ActualQuantity = sItem.ActualQuantity,
                                ReturnedQuantity = sItem.ReturnedQuantity,
                                UnitPrice = sItem.UnitPrice,
                                Amount = sItem.Amount,
                                DiscountRate = sItem.DiscountRate,
                                DiscountAmount = sItem.DiscountAmount,
                                VatRate = sItem.VatRate,
                                VatAmount = sItem.VatAmount,
                                TotalAmount = sItem.TotalAmount,
                                IsSentToKitchen = sItem.IsSentToKitchen,
                                IsReturned = sItem.IsReturned,
                                IsChecked = sItem.IsChecked,
                                IsCancelled = sItem.IsCancelled,
                                Note = sItem.Note,
                                SelectedOptionsJson = sItem.SelectedOptionsJson,
                                CreatedOnDate = now,
                                UpdatedOnDate = now
                            };
                            itemRepo.Add(newItem);
                            targetItems.Add(newItem);

                            // Update kitchen request details to newItem.Id
                            var detailsToUpdate = await kitchenRequestDetailRepo.Get(x => x.OrderDetailId == sItem.Id).ToListAsync();
                            foreach (var det in detailsToUpdate)
                            {
                                det.OrderDetailId = newItem.Id;
                                kitchenRequestDetailRepo.Update(det);
                            }
                        }

                        // Delete original source item
                        itemRepo.Delete(sItem);
                    }

                    // Update source order status to Merged
                    var srcOldStatus = srcOrder.Status;
                    srcOrder.Status = OrderStatusConstants.Merged;
                    srcOrder.Note = (srcOrder.Note ?? "") + $" [Đã gộp vào order {targetOrder.OrderCode}]";
                    srcOrder.UpdatedOnDate = now;
                    orderRepo.Update(srcOrder);

                    await _logger.InfoAsync($"Order status changed: OrderId={srcId}, BranchCode={branchCode}, Action=Merge, OldStatus={srcOldStatus}, NewStatus={OrderStatusConstants.Merged}");
                }

                // If tableIds is explicitly provided, update target order tables
                if (tableIds != null && tableIds.Any())
                {
                    // Delete target order's current tables
                    var targetTables = await orderTableRepo.Get(x => x.OrderId == targetOrderId && x.BranchCode == branchCode).ToListAsync();
                    foreach (var tbl in targetTables)
                    {
                        orderTableRepo.Delete(tbl);
                    }

                    // Add new mappings for the specified TableIds
                    Guid newFloorId = Guid.Empty;
                    string? newFloorName = null;
                    var tableRepo = _unit.GetRepository<DiningTable>();

                    foreach (var tId in tableIds)
                    {
                        var table = await tableRepo.Get(x => x.Id == tId && x.Status != 0).FirstOrDefaultAsync();
                        if (table == null) continue;

                        if (newFloorId == Guid.Empty)
                        {
                            newFloorId = table.FloorPlanId;
                            var floor = await _unit.GetRepository<FloorPlan>().Get(x => x.Id == newFloorId).FirstOrDefaultAsync();
                            newFloorName = floor?.Name;
                        }

                        orderTableRepo.Add(new SalesOrderTable
                        {
                            Id = Guid.NewGuid(),
                            BranchCode = branchCode,
                            OrderId = targetOrderId,
                            TableId = tId,
                            TableCode = table.Code,
                            TableName = table.Name,
                            FloorId = table.FloorPlanId,
                            CreatedOnDate = now
                        });
                    }

                    if (newFloorId != Guid.Empty)
                    {
                        targetOrder.FloorId = newFloorId;
                        targetOrder.FloorName = newFloorName;
                    }
                }

                // Determine targetOrder status
                var targetOldStatus = targetOrder.Status;
                if (statuses.Contains(OrderStatusConstants.Serving) || 
                    statuses.Contains(OrderStatusConstants.WaitingPayment) || 
                    statuses.Contains(OrderStatusConstants.Checked))
                {
                    targetOrder.Status = OrderStatusConstants.Serving;
                }
                else
                {
                    targetOrder.Status = OrderStatusConstants.Reserved;
                }

                targetOrder.SelectedPromotionCode = null;
                targetOrder.UpdatedOnDate = now;
                orderRepo.Update(targetOrder);

                // Resolve final target TableName
                var finalTableName = "Bàn";
                if (targetOrder.ServiceType == "DineIn")
                {
                    var targetTables = await orderTableRepo.Get(x => x.OrderId == targetOrderId && x.BranchCode == branchCode).ToListAsync();
                    if (targetTables.Any())
                    {
                        finalTableName = string.Join(" + ", targetTables.OrderBy(ot => ot.TableName).Select(ot => ot.TableName));
                    }
                    else
                    {
                        finalTableName = "Tại bàn";
                    }
                }
                else if (targetOrder.ServiceType == "TakeAway")
                {
                    finalTableName = "Mang về";
                }
                else if (targetOrder.ServiceType == "Delivery")
                {
                    finalTableName = "Giao hàng";
                }

                // Update kitchen requests OrderId, OrderCode, and TableName
                var kitchenRequestsToUpdate = await kitchenRequestRepo.Get(x => mergedRequestIds.Contains(x.Id)).ToListAsync();
                foreach (var req in kitchenRequestsToUpdate)
                {
                    req.OrderId = targetOrderId;
                    req.OrderCode = targetOrder.OrderCode;
                    req.TableName = finalTableName;
                    kitchenRequestRepo.Update(req);
                }

                await _unit.SaveAsync();

                if (targetOrder.Status != targetOldStatus)
                {
                    await _logger.InfoAsync($"Order status changed: OrderId={targetOrderId}, BranchCode={branchCode}, Action=Merge, OldStatus={targetOldStatus}, NewStatus={targetOrder.Status}");
                }

                // Recalculate totals
                await CalculateOrderTotal(targetOrderId, branchCode);

                await trans.CommitAsync();

                await BroadcastSalesEventAsync(branchCode, new SalesRealtimeEventDto
                {
                    EventType = "TablesMerged",
                    OrderId = targetOrderId,
                    OrderCode = targetOrder.OrderCode,
                    NewStatus = targetOrder.Status,
                    ServiceType = targetOrder.ServiceType,
                    TableName = finalTableName
                });

                // Broadcast kitchen updates via SignalR
                await _kitchenHubSender.SendRequestsUpdatedAsync(branchCode);

                return new Response(Code.Success, "Gộp bàn thành công!");
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("SalesOrderService.MergeOrders", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi: " + ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════
        // SPLIT ORDER
        // ═══════════════════════════════════════════════════════

        public async Task<Response> SplitOrder(Guid sourceOrderId, Guid targetTableId, bool isNewBill, List<SplitItemRequest> itemsToSplit, int customerCount, string branchCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(branchCode))
                    return new Response(Code.BadRequest, "Mã chi nhánh không được để trống!");

                if (sourceOrderId == Guid.Empty || itemsToSplit == null || !itemsToSplit.Any())
                    return new Response(Code.BadRequest, "Thông tin tách không hợp lệ!");

                var orderRepo = _unit.GetRepository<SalesOrder>();
                var sourceOrder = await orderRepo.Get(x => x.Id == sourceOrderId && x.BranchCode == branchCode).FirstOrDefaultAsync();
                if (sourceOrder == null)
                    return new Response(Code.NotFound, "Hóa đơn gốc không tồn tại!");

                if (sourceOrder.Status == "Paid" || sourceOrder.Status == "Completed" || sourceOrder.Status == "Cancelled" || sourceOrder.Status == "Merged")
                {
                    return new Response(Code.BadRequest, "Không thể tách hóa đơn đã thanh toán hoặc đã hủy/gộp!");
                }

                if (sourceOrder.CustomerCount > 1 && sourceOrder.CustomerCount <= customerCount)
                    return new Response(Code.BadRequest, "Số lượng khách tách không hợp lệ (hóa đơn gốc phải còn ít nhất 1 khách)!");

                using var trans = await _unit.BeginTransactionAsync();
                var itemRepo = _unit.GetRepository<SalesOrderItem>();
                var orderTableRepo = _unit.GetRepository<SalesOrderTable>();
                var tableRepo = _unit.GetRepository<DiningTable>();
                var kitchenRequestRepo = _unit.GetRepository<KitchenRequest>();
                var kitchenRequestDetailRepo = _unit.GetRepository<KitchenRequestDetail>();

                DiningTable? targetTable = null;
                if (targetTableId != Guid.Empty)
                {
                    targetTable = await tableRepo.Get(x => x.Id == targetTableId && x.Status != 0).FirstOrDefaultAsync();
                    if (targetTable == null)
                        return new Response(Code.NotFound, "Bàn đích không tồn tại hoặc đã bị khóa!");

                    var targetBranch = await _unit.GetRepository<Branchs>().Get(b => b.Id == targetTable.BranchId && b.Status != 0).FirstOrDefaultAsync();
                    if (targetBranch == null || targetBranch.Code != branchCode)
                    {
                        return new Response(Code.BadRequest, "Bàn đích không thuộc chi nhánh này!");
                    }
                }

                var headerMapping = new Dictionary<Guid, Guid>();
                int newHeadersCount = 0;

                var sourceItems = await itemRepo.Get(x => x.OrderId == sourceOrderId && x.BranchCode == branchCode).ToListAsync();
                var now = DateTime.Now;

                foreach (var split in itemsToSplit)
                {
                    await _logger.InfoAsync($"[SplitOrder DIAGNOSTIC] split.Id={split.Id}, split.ItemId={split.ItemId}, split.Quantity={split.Quantity}");
                }

                // Validate split item quantities and check remaining items
                bool allItemsSplit = true;
                var validatedMatchedItems = new List<SalesOrderItem>();
                foreach (var split in itemsToSplit)
                {
                    var original = (split.Id.HasValue && split.Id.Value != Guid.Empty)
                        ? sourceItems.FirstOrDefault(x => x.Id == split.Id.Value)
                        : sourceItems.FirstOrDefault(x => x.ItemId == split.ItemId && !validatedMatchedItems.Contains(x));
                    if (original == null)
                        return new Response(Code.BadRequest, $"Món ăn không tồn tại trong hóa đơn!");

                    validatedMatchedItems.Add(original);

                    double originalQty = original.Quantity;
                    double originalServed = original.ServedQty;
                    double originalCancelled = original.CancelledQuantity + original.SoldOutQty;
                    double originalWaiting = Math.Max(0, originalQty - originalServed - originalCancelled);

                    double splitQty = split.Quantity;
                    double splitServed = split.ActualQuantity; // Map request's ActualQuantity to ServedQty
                    double splitCancelled = split.CancelledQuantity;
                    double splitWaiting = splitQty - splitServed - splitCancelled;

                    // If caller did not provide detailed breakdown (splitServed == 0 && splitCancelled == 0)
                    // but splitWaiting > originalWaiting, auto-allocate according to available original breakdown
                    if (splitServed == 0 && splitCancelled == 0 && splitWaiting > originalWaiting)
                    {
                        splitWaiting = Math.Max(0, originalWaiting);
                        double remaining = splitQty - splitWaiting;
                        splitServed = Math.Min(remaining, originalServed);
                        remaining -= splitServed;
                        splitCancelled = Math.Min(remaining, originalCancelled);

                        split.ActualQuantity = splitServed;
                        split.CancelledQuantity = splitCancelled;
                    }

                    if (splitQty <= 0 || splitQty > originalQty)
                        return new Response(Code.BadRequest, $"Số lượng tách của món '{original.ItemName}' không hợp lệ!");

                    if (splitServed < 0 || splitServed > originalServed || splitServed > splitQty)
                        return new Response(Code.BadRequest, $"Số lượng đã trả tách của món '{original.ItemName}' không hợp lệ!");

                    if (splitCancelled < 0 || splitCancelled > originalCancelled || splitCancelled > (splitQty - splitServed))
                        return new Response(Code.BadRequest, $"Số lượng hủy tách của món '{original.ItemName}' không hợp lệ!");

                    if (splitWaiting < 0 || splitWaiting > originalWaiting)
                        return new Response(Code.BadRequest, $"Số lượng chờ tách của món '{original.ItemName}' không hợp lệ!");
                }

                var matchedRequests = new List<SplitItemRequest>();
                foreach (var original in sourceItems)
                {
                    var splitReq = itemsToSplit.FirstOrDefault(x => !matchedRequests.Contains(x) && ((x.Id.HasValue && x.Id.Value != Guid.Empty)
                        ? x.Id.Value == original.Id
                        : x.ItemId == original.ItemId));
                    
                    if (splitReq != null)
                    {
                        matchedRequests.Add(splitReq);
                    }

                    double splitQty = splitReq?.Quantity ?? 0;
                    if (original.Quantity > splitQty)
                    {
                        allItemsSplit = false;
                        break;
                    }
                }
                if (allItemsSplit)
                {
                    return new Response(Code.BadRequest, "Không được tách toàn bộ các món ăn ra khỏi hóa đơn gốc!");
                }

                // Determine target status
                string targetStatus = sourceOrder.Status == OrderStatusConstants.Reserved
                    ? OrderStatusConstants.Reserved
                    : OrderStatusConstants.Serving;

                // Get target order
                bool isTargetOrderNewlyCreated = false;
                SalesOrder targetOrder = null!;
                if (isNewBill || targetTableId == Guid.Empty)
                {
                    isTargetOrderNewlyCreated = true;
                    if (targetTable != null)
                    {
                        // Check if target table belongs to source order tables
                        var sourceOrderTables = await orderTableRepo.Get(ot => ot.OrderId == sourceOrderId && ot.BranchCode == branchCode).ToListAsync();
                        bool isSplittingFromSourceOrderTables = sourceOrderTables.Any(ot => ot.TableId == targetTableId);
                        if (isSplittingFromSourceOrderTables)
                        {
                            if (sourceOrderTables.Count > 1)
                            {
                                var tableToRemove = sourceOrderTables.FirstOrDefault(ot => ot.TableId == targetTableId);
                                if (tableToRemove != null)
                                {
                                    orderTableRepo.Delete(tableToRemove);
                                }
                            }
                            // If sourceOrderTables.Count <= 1 (splitting a new bill on the same single table),
                            // we do NOT delete the table link from sourceOrder. Both bills remain on the same table.
                        }

                        var capRes = await ValidateTableCapacityForOrder(branchCode, new List<Guid> { targetTableId }, customerCount, new List<Guid> { sourceOrderId });
                        if (capRes.Code != Code.Success)
                        {
                            return capRes;
                        }
                    }

                    var orderCode = await GenerateOrderCode(branchCode);
                    targetOrder = new SalesOrder
                    {
                        Id = Guid.NewGuid(),
                        BranchCode = branchCode,
                        OrderCode = orderCode,
                        OrderDate = now.Date,
                        OpenTime = now,
                        FloorId = targetTable != null ? targetTable.FloorPlanId : sourceOrder.FloorId,
                        CustomerCount = customerCount > 0 ? customerCount : 1,
                        ServiceType = targetTable != null ? ServiceTypeConstants.DineIn : sourceOrder.ServiceType,
                        Status = targetStatus,
                        Note = $"Tách ra từ order {sourceOrder.OrderCode}",
                        CreatedOnDate = now,
                        CreatedByUserCode = "system",
                        CreatedByUserName = "System"
                    };
                    orderRepo.Add(targetOrder);

                    if (targetTable != null)
                    {
                        orderTableRepo.Add(new SalesOrderTable
                        {
                            Id = Guid.NewGuid(),
                            BranchCode = branchCode,
                            OrderId = targetOrder.Id,
                            TableId = targetTable.Id,
                            TableCode = targetTable.Code,
                            TableName = targetTable.Name,
                            FloorId = targetTable.FloorPlanId,
                            CreatedOnDate = now
                        });
                    }
                }
                else
                {
                    targetOrder = await orderTableRepo.Get(x => x.TableId == targetTableId && x.BranchCode == branchCode)
                        .Join(orderRepo.Get(o => o.BranchCode == branchCode && !OrderStatusConstants.InactiveStatuses.Contains(o.Status)),
                               ot => ot.OrderId,
                               o => o.Id,
                               (ot, o) => o)
                        .FirstOrDefaultAsync();

                    if (targetOrder == null)
                    {
                        isTargetOrderNewlyCreated = true;
                        var capRes = await ValidateTableCapacityForOrder(branchCode, new List<Guid> { targetTableId }, customerCount, new List<Guid> { sourceOrderId });
                        if (capRes.Code != Code.Success)
                        {
                            return capRes;
                        }

                        var orderCode = await GenerateOrderCode(branchCode);
                        targetOrder = new SalesOrder
                        {
                            Id = Guid.NewGuid(),
                            BranchCode = branchCode,
                            OrderCode = orderCode,
                            OrderDate = now.Date,
                            OpenTime = now,
                            FloorId = targetTable!.FloorPlanId,
                            CustomerCount = customerCount > 0 ? customerCount : 1,
                            ServiceType = ServiceTypeConstants.DineIn,
                            Status = targetStatus,
                            Note = $"Tách ra từ order {sourceOrder.OrderCode}",
                            CreatedOnDate = now,
                            CreatedByUserCode = "system",
                            CreatedByUserName = "System"
                        };
                        orderRepo.Add(targetOrder);

                        orderTableRepo.Add(new SalesOrderTable
                        {
                            Id = Guid.NewGuid(),
                            BranchCode = branchCode,
                            OrderId = targetOrder.Id,
                            TableId = targetTable.Id,
                            TableCode = targetTable.Code,
                            TableName = targetTable.Name,
                            FloorId = targetTable.FloorPlanId,
                            CreatedOnDate = now
                        });
                    }
                    else
                    {
                        var capRes = await ValidateTableCapacityForOrder(branchCode, new List<Guid> { targetTableId }, targetOrder.CustomerCount + customerCount, new List<Guid> { targetOrder.Id, sourceOrderId });
                        if (capRes.Code != Code.Success)
                        {
                            return capRes;
                        }

                        targetOrder.CustomerCount += customerCount;
                        targetOrder.Status = targetStatus; // Target order also transitions status
                        orderRepo.Update(targetOrder);
                    }
                }

                var targetItems = await itemRepo.Get(x => x.OrderId == targetOrder.Id && x.BranchCode == branchCode).ToListAsync();

                // Process split
                var processedMatchedItems = new List<SalesOrderItem>();
                var newlyAddedItems = new List<SalesOrderItem>();
                foreach (var split in itemsToSplit)
                {
                    var original = (split.Id.HasValue && split.Id.Value != Guid.Empty)
                        ? sourceItems.First(x => x.Id == split.Id.Value)
                        : sourceItems.First(x => x.ItemId == split.ItemId && !processedMatchedItems.Contains(x));

                    processedMatchedItems.Add(original);

                    double splitQty = split.Quantity;
                    double splitServed = split.ActualQuantity;
                    double splitCancelled = split.CancelledQuantity;

                    // Deduct splitCancelled from original.CancelledQuantity and original.SoldOutQty
                    double remainingSplitCancelled = splitCancelled;
                    double deductCancelled = Math.Min(original.CancelledQuantity, remainingSplitCancelled);
                    original.CancelledQuantity -= deductCancelled;
                    remainingSplitCancelled -= deductCancelled;

                    double deductSoldOut = Math.Min(original.SoldOutQty, remainingSplitCancelled);
                    original.SoldOutQty -= deductSoldOut;
                    remainingSplitCancelled -= deductSoldOut;

                    original.Quantity -= splitQty;
                    original.ServedQty -= splitServed;
                    original.RemainingQty = original.Quantity - original.ServedQty - original.SoldOutQty;
                    if (original.RemainingQty < 0) original.RemainingQty = 0;
                    original.SentQty = original.IsSentToKitchen == 1 ? original.Quantity : 0;
                    original.KitchenStatus = DetermineKitchenStatus(original.Quantity, original.ServedQty, original.SoldOutQty, original.CancelledQuantity, original.IsSentToKitchen, original.SentQty);

                    original.ActualQuantity = 0; // Reset checked quantities since it's back to Serving
                    original.IsChecked = 0;

                    var splitMiRepo = _unit.GetRepository<MenuItem>();
                    var splitMi = await splitMiRepo.Get(x => x.Id == original.ItemId).FirstOrDefaultAsync();

                    original.Amount = Math.Round((decimal)original.Quantity * original.UnitPrice, 0, MidpointRounding.AwayFromZero);
                    var effQty3124 = (decimal)(original.Quantity > 0 ? original.Quantity : 1);
                    var unitVat3124 = CalculateUnitVat(original.UnitPrice, original.VatRate, splitMi, original.SelectedOptionsJson);
                    var unitGross3124 = original.UnitPrice + unitVat3124;
                    // Fix rounding: tính discount tại cấp unit trước từ giá sau thuế (unitGross)
                    var unitDisc3103 = original.DiscountRate > 0 ? Math.Round(unitGross3124 * (decimal)(original.DiscountRate / 100), 0, MidpointRounding.AwayFromZero) : 0m;
                    original.DiscountAmount = unitDisc3103 * (decimal)original.Quantity;

                    var unitTotal3124 = Math.Max(0, unitGross3124 - unitDisc3103);
                    original.VatAmount = Math.Round(unitVat3124 * (decimal)original.Quantity, 0, MidpointRounding.AwayFromZero);
                    original.TotalAmount = Math.Round(unitTotal3124 * (decimal)original.Quantity, 0, MidpointRounding.AwayFromZero);
                    original.UpdatedOnDate = now;

                    if (original.Quantity <= 0)
                    {
                        itemRepo.Delete(original);
                    }
                    else
                    {
                        itemRepo.Update(original);
                    }

                    // Add to target
                    var amt = (decimal)splitQty * original.UnitPrice;
                    var discAmt = Math.Round(unitDisc3103 * (decimal)splitQty, 0, MidpointRounding.AwayFromZero);
                    var vatAmt = Math.Round(unitVat3124 * (decimal)splitQty, 0, MidpointRounding.AwayFromZero);
                    var totAmt = Math.Round(unitTotal3124 * (decimal)splitQty, 0, MidpointRounding.AwayFromZero);

                    var match = targetItems.FirstOrDefault(x => x.ItemId == original.ItemId
                                                             && x.SelectedOptionsJson == original.SelectedOptionsJson
                                                             && x.UnitPrice == original.UnitPrice
                                                             && x.IsSentToKitchen == original.IsSentToKitchen
                                                             && x.Note == original.Note
                                                             && x.PromotionCode == original.PromotionCode
                                                             && x.DiscountRate == original.DiscountRate
                                                             && x.IsCancelled == 0);
                    SalesOrderItem targetItem;
                    if (match != null)
                    {
                        match.Quantity += splitQty;
                        match.ServedQty += splitServed;
                        match.CancelledQuantity += deductCancelled;
                        match.SoldOutQty += deductSoldOut;
                        match.RemainingQty = match.Quantity - match.ServedQty - match.SoldOutQty;
                        if (match.RemainingQty < 0) match.RemainingQty = 0;
                        match.SentQty = match.IsSentToKitchen == 1 ? match.Quantity : 0;
                        match.KitchenStatus = DetermineKitchenStatus(match.Quantity, match.ServedQty, match.SoldOutQty, match.CancelledQuantity, match.IsSentToKitchen, match.SentQty);

                        match.ActualQuantity = 0;
                        match.IsChecked = 0;
                        match.Amount = Math.Round((decimal)match.Quantity * match.UnitPrice, 0, MidpointRounding.AwayFromZero);
                        
                        var unitVat3152 = Math.Round(match.UnitPrice * (decimal)(match.VatRate / 100), 0, MidpointRounding.AwayFromZero);
                        var unitGross3152 = match.UnitPrice + unitVat3152;
                        var unitDisc3152 = Math.Round(unitGross3152 * (decimal)(match.DiscountRate / 100), 0, MidpointRounding.AwayFromZero);
                        match.DiscountAmount = unitDisc3152 * (decimal)match.Quantity;
                        var unitTotal3152 = Math.Max(0, unitGross3152 - unitDisc3152);
                        match.VatAmount = Math.Round(unitVat3152 * (decimal)match.Quantity, 0, MidpointRounding.AwayFromZero);
                        match.TotalAmount = Math.Round(unitTotal3152 * (decimal)match.Quantity, 0, MidpointRounding.AwayFromZero);
                        match.UpdatedOnDate = now;
                        if (!newlyAddedItems.Contains(match))
                        {
                            itemRepo.Update(match);
                        }
                        targetItem = match;
                    }
                    else
                    {
                        var lineNo = targetItems.Any() ? targetItems.Max(x => x.LineNo) + 1 : 1;
                        var newItem = new SalesOrderItem
                        {
                            Id = Guid.NewGuid(),
                            BranchCode = branchCode,
                            OrderId = targetOrder.Id,
                            LineNo = lineNo,
                            ItemId = original.ItemId,
                            ItemCode = original.ItemCode,
                            ItemName = original.ItemName,
                            UnitName = original.UnitName,
                            Quantity = splitQty,
                            ServedQty = splitServed,
                            CancelledQuantity = deductCancelled,
                            SoldOutQty = deductSoldOut,
                            SentQty = original.IsSentToKitchen == 1 ? splitQty : 0,
                            RemainingQty = Math.Max(0, splitQty - splitServed - deductSoldOut),
                            KitchenStatus = DetermineKitchenStatus(splitQty, splitServed, deductSoldOut, deductCancelled, original.IsSentToKitchen, original.IsSentToKitchen == 1 ? splitQty : 0),
                            ActualQuantity = 0,
                            UnitPrice = original.UnitPrice,
                            Amount = amt,
                            DiscountRate = original.DiscountRate,
                            DiscountAmount = discAmt,
                            VatRate = original.VatRate,
                            VatAmount = vatAmt,
                            TotalAmount = totAmt,
                            IsSentToKitchen = original.IsSentToKitchen,
                            IsReturned = original.IsReturned,
                            IsChecked = 0,
                            IsCancelled = original.IsCancelled,
                            Note = original.Note,
                            SelectedOptionsJson = original.SelectedOptionsJson,
                            CreatedOnDate = now,
                            UpdatedOnDate = now
                        };
                        itemRepo.Add(newItem);
                        targetItems.Add(newItem);
                        newlyAddedItems.Add(newItem);
                        targetItem = newItem;
                    }

                    // Split/Move Kitchen Request Details
                    var originalDetails = await kitchenRequestDetailRepo.Get(x => x.OrderDetailId == original.Id).ToListAsync();
                    double remainingSplitQty = splitQty;

                    foreach (var detail in originalDetails)
                    {
                        if (remainingSplitQty <= 0) break;

                        double splitDetQty = Math.Min(remainingSplitQty, detail.RequestQty);
                        remainingSplitQty -= splitDetQty;

                        double splitDetServed = Math.Min(splitDetQty, detail.ServedQty);
                        double splitDetSoldOut = Math.Min(splitDetQty - splitDetServed, detail.SoldOutQty);

                        detail.RequestQty -= splitDetQty;
                        detail.ServedQty -= splitDetServed;
                        detail.SoldOutQty -= splitDetSoldOut;
                        detail.RemainingQty = detail.RequestQty - detail.ServedQty - detail.SoldOutQty;
                        if (detail.RemainingQty < 0) detail.RemainingQty = 0;

                        if (detail.RequestQty <= 0)
                        {
                            kitchenRequestDetailRepo.Delete(detail);
                        }
                        else
                        {
                            kitchenRequestDetailRepo.Update(detail);
                        }

                        // Find or Create target KitchenRequest header
                        Guid newHeaderId = Guid.Empty;
                        if (!headerMapping.TryGetValue(detail.KitchenRequestId, out newHeaderId))
                        {
                            var oldHeader = await kitchenRequestRepo.Get(x => x.Id == detail.KitchenRequestId).FirstOrDefaultAsync();
                            if (oldHeader != null)
                            {
                                var newHeader = new KitchenRequest
                                {
                                    Id = Guid.NewGuid(),
                                    RequestCode = "",
                                    OrderId = targetOrder.Id,
                                    OrderCode = targetOrder.OrderCode,
                                    BranchCode = branchCode,
                                    KitchenAreaId = oldHeader.KitchenAreaId,
                                    ServiceType = targetOrder.ServiceType,
                                    TableId = targetTable?.Id,
                                    TableName = targetTable?.Name,
                                    RequestedAt = oldHeader.RequestedAt,
                                    RequestedBy = oldHeader.RequestedBy,
                                    Status = oldHeader.Status
                                };

                                var count = await kitchenRequestRepo.Get(x => x.OrderId == targetOrder.Id).CountAsync() + newHeadersCount;
                                newHeader.RequestCode = $"{targetOrder.OrderCode}-{(count + 1):D3}";

                                kitchenRequestRepo.Add(newHeader);
                                newHeaderId = newHeader.Id;
                                headerMapping[detail.KitchenRequestId] = newHeaderId;
                                newHeadersCount++;
                            }
                        }

                        if (newHeaderId != Guid.Empty)
                        {
                            var newDetail = new KitchenRequestDetail
                            {
                                Id = Guid.NewGuid(),
                                KitchenRequestId = newHeaderId,
                                OrderDetailId = targetItem.Id,
                                MenuItemId = detail.MenuItemId,
                                MenuItemName = detail.MenuItemName,
                                RequestQty = splitDetQty,
                                ServedQty = splitDetServed,
                                SoldOutQty = splitDetSoldOut,
                                RemainingQty = splitDetQty - splitDetServed - splitDetSoldOut,
                                Status = detail.Status
                            };
                            kitchenRequestDetailRepo.Add(newDetail);
                        }
                    }
                }

                sourceOrder.CustomerCount = Math.Max(1, sourceOrder.CustomerCount - customerCount);
                sourceOrder.Status = targetStatus;
                sourceOrder.SelectedPromotionCode = null;
                sourceOrder.UpdatedOnDate = now;
                orderRepo.Update(sourceOrder);

                targetOrder.SelectedPromotionCode = null;
                targetOrder.UpdatedOnDate = now;
                // Only call Update() if targetOrder was NOT newly created in this operation.
                // If targetOrder was newly Add()-ed (isTargetOrderNewlyCreated = true), calling Update()
                // changes its state to Modified, causing EF Core to generate UPDATE instead of INSERT -> 0 rows affected -> concurrency error.
                if (!isTargetOrderNewlyCreated)
                {
                    orderRepo.Update(targetOrder);
                }
                // Clean up empty KitchenRequests from sourceOrderId
                var sourceRequestIds = await kitchenRequestRepo.Get(x => x.OrderId == sourceOrderId).Select(x => x.Id).ToListAsync();
                foreach (var reqId in sourceRequestIds)
                {
                    var hasDetails = await kitchenRequestDetailRepo.Get(x => x.KitchenRequestId == reqId).AnyAsync();
                    if (!hasDetails)
                    {
                        var req = await kitchenRequestRepo.Get(x => x.Id == reqId).FirstOrDefaultAsync();
                        if (req != null)
                        {
                            kitchenRequestRepo.Delete(req);
                        }
                    }
                }

                await _unit.SaveAsync();
                // without conflicting with stale tracked entities from the split operation.
                // Without this, EF Core may try to UPDATE newly-inserted items (causing concurrency errors).
                _unit.DataContext.ChangeTracker.Clear();

                // Recalculate both orders totals
                await CalculateOrderTotal(sourceOrderId, branchCode);
                await CalculateOrderTotal(targetOrder.Id, branchCode);

                await trans.CommitAsync();

                return new Response(Code.Success, "Tách bàn thành công!");
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("SalesOrderService.SplitOrder", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi: " + ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════
        // GET CHECK ITEMS
        // ═══════════════════════════════════════════════════════

        public async Task<Response> GetCheckItems(Guid orderId, string branchCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(branchCode))
                    return new Response(Code.BadRequest, "Mã chi nhánh không được để trống!");

                if (orderId == Guid.Empty)
                    return new Response(Code.BadRequest, "OrderId không hợp lệ!");

                var orderRepo = _unit.GetRepository<SalesOrder>();
                var order = await orderRepo.Get(x => x.Id == orderId && x.BranchCode == branchCode).FirstOrDefaultAsync();
                if (order == null)
                    return new Response(Code.NotFound, "Order không tồn tại!");



                var itemRepo = _unit.GetRepository<SalesOrderItem>();
                var items = await itemRepo.Get(x => x.OrderId == orderId && x.BranchCode == branchCode && x.IsCancelled == 0).OrderBy(x => x.LineNo).ToListAsync();

                // Map to check models
                var checkItems = items.Select(i => new SaleBillItemModel
                {
                    Id = i.Id,
                    ItemId = i.ItemId,
                    ItemCode = i.ItemCode,
                    ItemName = i.ItemName,
                    UnitName = i.UnitName,
                    Quantity = i.Quantity,
                    Price = i.UnitPrice,
                    Amount = i.Amount,
                    ServedQuantity = i.IsChecked == 1 ? i.ActualQuantity : i.ServedQty,
                    RemainingQuantity = i.Quantity - (i.IsChecked == 1 ? i.ActualQuantity : i.ServedQty),
                    Note = i.Note,
                    DiscountRate = i.DiscountRate,
                    DiscountAmount = i.DiscountAmount,
                    VatRate = i.VatRate,
                    VatAmount = i.VatAmount,
                    TotalAmount = i.TotalAmount,
                    IsSentToKitchen = i.IsSentToKitchen,
                    KitchenStatus = i.KitchenStatus,
                    SentQty = i.SentQty,
                    ServedQty = i.ServedQty,
                    SoldOutQty = i.SoldOutQty,
                    RemainingQty = i.RemainingQty,
                    CancelledQuantity = i.CancelledQuantity,
                    ActualQuantity = i.IsChecked == 1 ? i.ActualQuantity : i.ServedQty,
                    ReturnedQuantity = i.IsChecked == 1 ? i.ReturnedQuantity : 0,
                    IsChecked = i.IsChecked
                }).ToList();

                var orderTableRepo = _unit.GetRepository<SalesOrderTable>();
                var orderTables = await orderTableRepo.Get(x => x.OrderId == orderId && x.BranchCode == branchCode).ToListAsync();
                var combinedTableName = string.Join(" + ", orderTables.OrderBy(ot => ot.TableName).Select(ot => ot.TableName));
                var combinedTableCode = string.Join(" + ", orderTables.OrderBy(ot => ot.TableName).Select(ot => ot.TableCode));

                var detail = new SaleBillDetailModel
                {
                    BillId = order.Id.ToString(),
                    BillCode = order.OrderCode,
                    Status = order.Status,
                    CustomerCount = order.CustomerCount,
                    OpenTime = order.OpenTime,
                    WaiterId = order.WaiterId,
                    WaiterName = order.WaiterName,
                    Items = checkItems,
                    SubTotal = order.SubTotal,
                    DiscountAmount = order.DiscountAmount,
                    VatAmount = order.VatAmount,
                    TotalAmount = order.TotalAmount,
                    TableName = combinedTableName,
                    TableCode = combinedTableCode,
                    TableIds = orderTables.Select(ot => ot.TableId).ToList(),
                    TableNames = orderTables.Select(ot => ot.TableName).ToList(),
                    SourceTableName = combinedTableName,
                    OriginalCustomerCount = order.CustomerCount,
                    SelectedPromotionCode = order.SelectedPromotionCode,
                    TherapyVoucherCode = order.TherapyVoucherCode
                };

                return new ResponseObject<SaleBillDetailModel>
                {
                    Code = Code.Success,
                    Message = "Lấy danh sách kiểm đồ thành công!",
                    Data = detail
                };
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("SalesOrderService.GetCheckItems", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi: " + ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════
        // CHECK ITEMS (CONFIRM CHECK)
        // ═══════════════════════════════════════════════════════

        public async Task<Response> CheckItems(Guid orderId, List<OrderItemCheckRequest> items, string branchCode, string? userCode = null, string? userName = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(branchCode))
                    return new Response(Code.BadRequest, "Mã chi nhánh không được để trống!");

                if (orderId == Guid.Empty || items == null || !items.Any())
                    return new Response(Code.BadRequest, "Thông tin kiểm đồ không hợp lệ!");

                var orderRepo = _unit.GetRepository<SalesOrder>();
                var order = await orderRepo.Get(x => x.Id == orderId && x.BranchCode == branchCode).FirstOrDefaultAsync();
                if (order == null)
                    return new Response(Code.NotFound, "Order không tồn tại!");

                if (order.Status == "Paid" || order.Status == "Completed" || order.Status == "Cancelled" || order.Status == "Merged")
                {
                    return new Response(Code.BadRequest, "Không thể kiểm đồ cho hóa đơn đã thanh toán hoặc đã hủy/gộp!");
                }

                if (items.Any(x => x.CheckedQuantity < 0))
                {
                    return new Response(Code.BadRequest, "Số lượng thực dùng không được nhỏ hơn 0!");
                }

                bool requireKitchenCheck = await IsRequireKitchenSendEnabled();
                if (requireKitchenCheck)
                {
                    // Check pending kitchen requests
                    var kitchenRequestIds = await _unit.GetRepository<KitchenRequest>()
                        .Get(r => r.OrderId == orderId)
                        .Select(r => r.Id)
                        .ToListAsync();

                    var hasPending = await _unit.GetRepository<KitchenRequestDetail>()
                        .Get(d => kitchenRequestIds.Contains(d.KitchenRequestId) && d.RemainingQty > 0)
                        .AnyAsync();
                    if (hasPending)
                    {
                        return new Response(Code.BadRequest, "Vui lòng kiểm đồ sau khi bếp hoàn thành các yêu cầu gọi món của đơn");
                    }
                }

                using var trans = await _unit.BeginTransactionAsync();

                var itemRepo = _unit.GetRepository<SalesOrderItem>();
                var existingItems = await itemRepo.Get(x => x.OrderId == orderId && x.BranchCode == branchCode).ToListAsync();
                
                bool isTakeAwayOrDelivery = string.Equals(order.ServiceType, ServiceTypeConstants.Delivery, StringComparison.OrdinalIgnoreCase) ||
                                            string.Equals(order.ServiceType, ServiceTypeConstants.TakeAway, StringComparison.OrdinalIgnoreCase) ||
                                            string.Equals(order.ServiceType, "Delivery", StringComparison.OrdinalIgnoreCase) ||
                                            string.Equals(order.ServiceType, "TakeAway", StringComparison.OrdinalIgnoreCase);

                var unfinishedItems = existingItems.Where(i => i.IsCancelled == 0 && (i.ServedQty + i.SoldOutQty < i.Quantity)).ToList();
                if (requireKitchenCheck && unfinishedItems.Any() && !isTakeAwayOrDelivery)
                {
                    return new Response(Code.BadRequest, "Tất cả các món phải được Bếp/Bar trả món hoặc báo hủy đầy đủ (SL đã trả + SL bếp hủy = SL order) mới có thể thực hiện kiểm đồ!");
                }

                var now = DateTime.Now;
                var kitchenRequestRepo = _unit.GetRepository<KitchenRequest>();
                var kitchenDetailRepo = _unit.GetRepository<KitchenRequestDetail>();

                // Complete all existing kitchen requests for this order since we are performing reconciliation/checking
                var existingRequests = await kitchenRequestRepo.Get(x => x.OrderId == orderId).ToListAsync();
                var existingReqIds = existingRequests.Select(x => x.Id).ToList();
                var existingDetails = await kitchenDetailRepo.Get(x => existingReqIds.Contains(x.KitchenRequestId)).ToListAsync();

                foreach (var detail in existingDetails)
                {
                    detail.ServedQty = detail.RequestQty;
                    detail.RemainingQty = 0;
                    detail.Status = "Completed";
                    kitchenDetailRepo.Update(detail);
                }

                foreach (var req in existingRequests)
                {
                    req.Status = "Completed";
                    kitchenRequestRepo.Update(req);
                }

                var orderTableRepo = _unit.GetRepository<SalesOrderTable>();
                var orderTables = await orderTableRepo.Get(x => x.OrderId == orderId && x.BranchCode == branchCode).ToListAsync();

                // Group the difference items by preparation area
                var defaultArea = await _unit.GetRepository<PreparationArea>().GetAll().FirstOrDefaultAsync();
                var defaultAreaId = defaultArea?.Id ?? Guid.Parse("5dc65bf9-b2c6-4357-bb03-24cd9a3b6791");

                var diffItems = new List<(SalesOrderItem Item, double Diff, Guid PrepAreaId)>();
                foreach (var check in items)
                {
                    var original = existingItems.FirstOrDefault(x => x.ItemId == check.ItemId && x.IsCancelled == 0);
                    if (original == null) continue;

                    double processedQty = original.IsChecked == 1 
                        ? original.ActualQuantity 
                        : (original.Quantity - original.SoldOutQty - original.CancelledQuantity);
                    double diff = check.CheckedQuantity - processedQty;

                    if (diff > 0)
                    {
                        var menuItem = await _unit.GetRepository<MenuItem>().Get(x => x.Id == original.ItemId).FirstOrDefaultAsync();
                        var areaId = menuItem?.PreparationAreaId;
                        if (!areaId.HasValue || areaId.Value == Guid.Empty)
                        {
                            areaId = defaultAreaId;
                        }
                        diffItems.Add((original, diff, areaId.Value));
                    }
                }

                if (diffItems.Any())
                {
                    var groupedDiffs = diffItems.GroupBy(x => x.PrepAreaId);
                    foreach (var group in groupedDiffs)
                    {
                        var areaId = group.Key;
                        var nextSeq = await GetNextKitchenRequestSequence(branchCode, order.OrderCode);
                        var kdRequestCode = $"{order.OrderCode}-{nextSeq:D3}";
                        var newHeaderId = Guid.NewGuid();

                        var kitchenRequest = new KitchenRequest
                        {
                            Id = newHeaderId,
                            BranchCode = branchCode,
                            OrderId = orderId,
                            OrderCode = order.OrderCode,
                            KitchenAreaId = areaId,
                            ServiceType = order.ServiceType,
                            TableId = orderTables.FirstOrDefault()?.TableId,
                            TableName = orderTables.FirstOrDefault()?.TableName,
                            RequestCode = kdRequestCode,
                            RequestedAt = now,
                            RequestedBy = "Kiểm đồ",
                            Status = "Completed"
                        };
                        kitchenRequestRepo.Add(kitchenRequest);

                        foreach (var diffInfo in group)
                        {
                            kitchenDetailRepo.Add(new KitchenRequestDetail
                            {
                                Id = Guid.NewGuid(),
                                KitchenRequestId = newHeaderId,
                                OrderDetailId = diffInfo.Item.Id,
                                MenuItemId = diffInfo.Item.ItemId,
                                MenuItemName = diffInfo.Item.ItemName,
                                RequestQty = diffInfo.Diff,
                                ServedQty = diffInfo.Diff,
                                RemainingQty = 0,
                                Status = "Completed"
                            });
                        }
                    }
                }

                foreach (var check in items)
                {
                    var original = existingItems.FirstOrDefault(x => x.ItemId == check.ItemId && x.IsCancelled == 0);
                    if (original == null) continue;
                    double checkedQty = check.CheckedQuantity;
                    if (checkedQty <= 0)
                    {
                        original.Quantity = 0;
                        original.ServedQty = 0;
                        original.SentQty = 0;
                        original.RemainingQty = 0;
                        original.SoldOutQty = 0;
                        original.CancelledQuantity = 0;
                        original.IsSentToKitchen = 0;
                        original.ActualQuantity = 0;
                        original.ReturnedQuantity = 0;
                        original.IsChecked = 1;
                        original.IsCancelled = 1;
                        original.KitchenStatus = "Cancelled";
                        original.UpdatedOnDate = now;
                    }
                    else
                    {
                        if (checkedQty < original.ServedQty)
                        {
                            // TH1: Kiểm đồ giảm SL thực dùng -> SL khách hủy = SL đã trả - SL thực dùng
                            original.ActualQuantity = checkedQty;
                            original.ReturnedQuantity = original.ServedQty - checkedQty;
                            original.IsChecked = 1;
                            original.UpdatedOnDate = now;
                        }
                        else if (checkedQty > original.ServedQty)
                        {
                            // TH2: Kiểm đồ tăng SL thực dùng -> Tăng SL order, SL đã gửi, SL đã trả, SL thực dùng tương ứng
                            double diff = checkedQty - original.ServedQty;
                            original.Quantity += diff;
                            original.SentQty += diff;
                            original.ServedQty += diff;
                            original.ActualQuantity = checkedQty;
                            original.ReturnedQuantity = 0;
                            original.IsChecked = 1;
                            original.UpdatedOnDate = now;
                        }
                        else
                        {
                            original.ActualQuantity = checkedQty;
                            original.ReturnedQuantity = 0;
                            original.IsChecked = 1;
                            original.UpdatedOnDate = now;
                        }
                    }
                    itemRepo.Update(original);
                }

                // Update order status to Checked
                var oldStatus = order.Status;
                order.Status = OrderStatusConstants.Checked;
                order.UpdatedOnDate = now;
                if (!string.IsNullOrEmpty(userCode)) order.UpdatedByUserCode = userCode;
                if (!string.IsNullOrEmpty(userName)) order.UpdatedByUserName = userName;
                orderRepo.Update(order);

                await _logger.InfoAsync($"Order status changed: OrderId={orderId}, BranchCode={branchCode}, Action=CheckItems, OldStatus={oldStatus}, NewStatus={OrderStatusConstants.Checked}");

                await _unit.SaveAsync();

                // Clear change tracker so CalculateOrderTotal loads fresh entities
                // without conflicting with tracked entities in CheckItems.
                _unit.DataContext.ChangeTracker.Clear();

                await CalculateOrderTotal(orderId, branchCode);
                await trans.CommitAsync();

                // Notify kitchen SignalR hub about the newly created kitchen requests
                await _kitchenHubSender.SendRequestsUpdatedAsync(branchCode);

                // Mock print check ticket
                await _logger.InfoAsync($"PrintCheckItemsTicket: OrderId={orderId}");

                return new Response(Code.Success, "Kiểm đồ thành công!");
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("SalesOrderService.CheckItems", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi: " + ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════
        // PROCESS PAYMENT
        // ═══════════════════════════════════════════════════════

        public async Task<Response> ProcessPayment(Guid orderId, string paymentType, decimal paymentAmount, decimal cashReceivedAmount, string? transactionId, string branchCode, string? cashierId = null, string? cashierName = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(branchCode))
                    return new Response(Code.BadRequest, "Mã chi nhánh không được để trống!");

                if (orderId == Guid.Empty)
                    return new Response(Code.BadRequest, "OrderId không hợp lệ!");

                var orderRepo = _unit.GetRepository<SalesOrder>();
                var order = await orderRepo.Get(x => x.Id == orderId && x.BranchCode == branchCode).FirstOrDefaultAsync();
                if (order == null)
                    return new Response(Code.NotFound, "Order không tồn tại!");

                bool requireKitchenPayCheck = await IsRequireKitchenSendEnabled();
                if (requireKitchenPayCheck)
                {
                    // Check pending kitchen requests
                    if (order.TotalAmount > 0 && order.Status != OrderStatusConstants.Checked)
                    {
                        var kitchenRequestIds = await _unit.GetRepository<KitchenRequest>()
                            .Get(r => r.OrderId == orderId)
                            .Select(r => r.Id)
                            .ToListAsync();

                        var hasPending = await _unit.GetRepository<KitchenRequestDetail>()
                            .Get(d => kitchenRequestIds.Contains(d.KitchenRequestId) && d.RemainingQty > 0)
                            .AnyAsync();
                        if (hasPending)
                        {
                            return new Response(Code.BadRequest, "Vui lòng gửi order cho bếp và thanh toán sau khi bếp hoàn thành các yêu cầu gọi món của đơn");
                        }
                    }
                }

                bool isTakeAwayOrDelivery = string.Equals(order.ServiceType, ServiceTypeConstants.Delivery, StringComparison.OrdinalIgnoreCase) ||
                                            string.Equals(order.ServiceType, ServiceTypeConstants.TakeAway, StringComparison.OrdinalIgnoreCase) ||
                                            string.Equals(order.ServiceType, "Delivery", StringComparison.OrdinalIgnoreCase) ||
                                            string.Equals(order.ServiceType, "TakeAway", StringComparison.OrdinalIgnoreCase);

                var payItems = await _unit.GetRepository<SalesOrderItem>()
                    .Get(x => x.OrderId == orderId && x.BranchCode == branchCode && x.IsCancelled == 0)
                    .ToListAsync();
                var payUnfinished = payItems.Where(i => (i.ServedQty + i.SoldOutQty < i.Quantity)).ToList();
                if (requireKitchenPayCheck && payUnfinished.Any() && !isTakeAwayOrDelivery)
                {
                    return new Response(Code.BadRequest, "Tất cả các món phải được Bếp/Bar trả món hoặc báo hủy đầy đủ (SL đã trả + SL bếp hủy = SL order) mới có thể thực hiện thanh toán!");
                }

                var oldStatus = order.Status;
                if (oldStatus == OrderStatusConstants.Paid)
                {
                    var exists = await _unit.GetRepository<SalesOrderPayment>()
                        .Get(x => x.OrderId == orderId && x.TransactionId == transactionId)
                        .AnyAsync();
                    if (exists)
                    {
                        return new ResponseObject<OrderActionResultDto>
                        {
                            Code = Code.Success,
                            Message = "Đơn hàng đã được thanh toán trước đó!",
                            Data = new OrderActionResultDto
                            {
                                OrderId = orderId,
                                OrderCode = order.OrderCode,
                                Status = order.Status,
                                Success = true,
                                Message = "Hóa đơn đã được thanh toán hoàn tất."
                            }
                        };
                    }
                }

                if (!OrderStatusConstants.CanTransition(oldStatus, OrderStatusConstants.Paid, "Payment"))
                    return new Response(Code.BadRequest, $"Không thể thanh toán order ở trạng thái '{oldStatus}'!");

                if (paymentType == "Cash" && Math.Round(cashReceivedAmount) < Math.Round(order.TotalAmount))
                    return new Response(Code.BadRequest, $"Số tiền mặt khách đưa ({cashReceivedAmount}) nhỏ hơn số tiền cần thanh toán ({order.TotalAmount})!");

                using var trans = await _unit.BeginTransactionAsync();

                var now = DateTime.Now;

                // Create payment
                var paymentRepo = _unit.GetRepository<SalesOrderPayment>();
                var payment = new SalesOrderPayment
                {
                    Id = Guid.NewGuid(),
                    BranchCode = branchCode,
                    OrderId = orderId,
                    TransactionId = transactionId,
                    PaymentType = paymentType,
                    PaymentAmount = paymentAmount,
                    CashReceivedAmount = cashReceivedAmount,
                    ChangeAmount = paymentType == "Cash" ? Math.Max(0, Math.Round(cashReceivedAmount) - Math.Round(order.TotalAmount)) : 0,
                    CreatedOnDate = now
                };
                paymentRepo.Add(payment);

                // Update Order status
                order.Status = OrderStatusConstants.Paid;
                order.PaymentTime = now;
                if (!string.IsNullOrEmpty(cashierId))
                {
                    order.CashierUserId = cashierId;
                    order.UpdatedByUserCode = cashierId;
                }
                if (!string.IsNullOrEmpty(cashierName))
                {
                    order.CashierUserName = cashierName;
                    order.UpdatedByUserName = cashierName;
                }
                order.UpdatedOnDate = now;
                orderRepo.Update(order);

                await _unit.SaveAsync();
                await trans.CommitAsync();

                await BroadcastSalesEventAsync(branchCode, new SalesRealtimeEventDto
                {
                    EventType = "OrderPaid",
                    OrderId = orderId,
                    OrderCode = order.OrderCode,
                    NewStatus = OrderStatusConstants.Paid,
                    ServiceType = order.ServiceType
                });

                await _logger.InfoAsync($"Order status changed: OrderId={orderId}, BranchCode={branchCode}, Action=Payment, OldStatus={oldStatus}, NewStatus={OrderStatusConstants.Paid}");

                return new ResponseObject<OrderActionResultDto>
                {
                    Code = Code.Success,
                    Message = "Thanh toán thành công!",
                    Data = new OrderActionResultDto
                    {
                        OrderId = orderId,
                        OrderCode = order.OrderCode,
                        Status = order.Status,
                        Success = true,
                        Message = "Hóa đơn đã được thanh toán hoàn tất."
                    }
                };
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("SalesOrderService.ProcessPayment", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi: " + ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════
        // GET CUSTOMERS
        // ═══════════════════════════════════════════════════════

        public async Task<Response> GetCustomers(string branchCode, string? keyword)
        {
            try
            {
                var bpRepo = _unit.GetRepository<BusinessPartner>();
                var query = bpRepo.Get(x => (x.PartnerTypes == 1 || x.PartnerTypes == 3) && x.Status == 1);

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var kw = keyword.Trim().ToLower();
                    query = query.Where(x => (x.Name != null && x.Name.ToLower().Contains(kw))
                                          || (x.Code != null && x.Code.ToLower().Contains(kw))
                                          || (x.MobilePhone != null && x.MobilePhone.Contains(kw)));
                }

                var list = await query.OrderBy(x => x.Name).Take(100).ToListAsync();
                return new ResponseObject<List<BusinessPartner>>
                {
                    Code = Code.Success,
                    Message = "Lấy danh sách khách hàng thành công!",
                    Data = list
                };
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("SalesOrderService.GetCustomers", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi: " + ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════
        // GET BRANCH EMPLOYEES
        // ═══════════════════════════════════════════════════════

        public async Task<Response> GetBranchEmployees(string branchCode, string? keyword = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(branchCode))
                    return new Response(Code.BadRequest, "Mã chi nhánh không được để trống!");

                var empRepo = _unit.GetRepository<Employee>();
                var query = empRepo.Get(x => x.BranchCode == branchCode && x.Status == 1 && x.IsEmployee == 1 && (x.Name != null && x.Name != ""));

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var kw = keyword.Trim().ToLower();
                    query = query.Where(x => (x.Name != null && x.Name.ToLower().Contains(kw))
                                          || (x.Code != null && x.Code.ToLower().Contains(kw))
                                          || (x.Phone != null && x.Phone.Contains(kw)));
                }

                var list = await query.OrderBy(x => x.Name)
                                      .Take(100)
                                      .ToListAsync();

                return new ResponseObject<List<Employee>>
                {
                    Code = Code.Success,
                    Message = "Lấy danh sách nhân viên thành công!",
                    Data = list
                };
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("SalesOrderService.GetBranchEmployees", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi: " + ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════
        // GET TOP USED MENU ITEMS
        // ═══════════════════════════════════════════════════════

        public async Task<Response> GetTopUsedMenuItems(string branchCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(branchCode))
                    return new Response(Code.BadRequest, "Mã chi nhánh không được để trống!");

                var branchRepo = _unit.GetRepository<Branchs>();
                var branch = await branchRepo.Get(x => x.Code == branchCode && x.Status != 0).FirstOrDefaultAsync();
                if (branch == null)
                    return new Response(Code.NotFound, "Chi nhánh không tồn tại!");

                var orderRepo = _unit.GetRepository<SalesOrder>();
                var itemRepo = _unit.GetRepository<SalesOrderItem>();
                var menuItemRepo = _unit.GetRepository<MenuItem>();
                var mibRepo = _unit.GetRepository<MenuItemBranch>();

                var oneWeekAgo = DateTime.Now.AddDays(-7);

                var popularItemIds = await orderRepo.Get(x => x.BranchCode == branchCode && x.Status == OrderStatusConstants.Paid && x.OrderDate >= oneWeekAgo.Date)
                    .Join(itemRepo.Get(x => x.BranchCode == branchCode && x.IsCancelled == 0),
                          o => o.Id,
                          i => i.OrderId,
                          (o, i) => i)
                    .GroupBy(i => i.ItemId)
                    .Select(g => new { ItemId = g.Key, Count = g.Sum(x => x.Quantity) })
                    .OrderByDescending(x => x.Count)
                    .Take(20)
                    .Select(x => x.ItemId)
                    .ToListAsync();

                var hiddenIds = await _visibility.GetHiddenIdsAsync(branchCode);
                var menuItemsQuery = from mi in menuItemRepo.Get(x => x.Status == 1 && !hiddenIds.Contains(x.Id))
                                     join mib in mibRepo.Get() on mi.Id equals mib.MenuItemId into mibJoin
                                     where !mibJoin.Any() || mibJoin.Any(x => x.BranchId == branch.Id)
                                     select mi;

                if (popularItemIds.Any())
                {
                    menuItemsQuery = menuItemsQuery.Where(x => popularItemIds.Contains(x.Id));
                }

                var list = await menuItemsQuery.Take(20).ToListAsync();

                if (list.Count < 20)
                {
                    var currentIds = list.Select(x => x.Id).ToList();
                    var extraQuery = from mi in menuItemRepo.Get(x => x.Status == 1 && !currentIds.Contains(x.Id) && !hiddenIds.Contains(x.Id))
                                      join mib in mibRepo.Get() on mi.Id equals mib.MenuItemId into mibJoin
                                      where !mibJoin.Any() || mibJoin.Any(x => x.BranchId == branch.Id)
                                      select mi;
                    var extra = await extraQuery.Take(20 - list.Count).ToListAsync();
                    list.AddRange(extra);
                }

                // Load tax rates and units
                var taxRepo = _unit.GetRepository<TaxRate>();
                var taxes = await taxRepo.Get(x => x.Status == 1).ToListAsync();
                var taxDict = taxes.ToDictionary(t => t.Id, t => (double)t.Rate);

                var unitRepo = _unit.GetRepository<Unit>();
                var units = await unitRepo.Get(x => x.Status != 0).ToListAsync();
                var unitDict = units.ToDictionary(u => u.Id, u => u.Name);

                foreach (var item in list)
                {
                    if (item.TaxRateId.HasValue && taxDict.TryGetValue(item.TaxRateId.Value, out var rate))
                    {
                        item.TaxRatePercent = rate;
                    }
                    if (item.UnitId.HasValue && unitDict.TryGetValue(item.UnitId.Value, out var uName))
                    {
                        item.UnitName = uName;
                    }
                }

                list = list.OrderBy(x => x.CourseOrder > 0 ? x.CourseOrder : 999999).ThenBy(x => x.Name).ToList();

                return new ResponseObject<List<MenuItem>>
                {
                    Code = Code.Success,
                    Message = "Lấy danh sách món ăn thường dùng thành công!",
                    Data = list
                };
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("SalesOrderService.GetTopUsedMenuItems", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi: " + ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════
        // GET MENU ITEMS FILTER
        // ═══════════════════════════════════════════════════════

        public async Task<Response> GetMenuItemsFilter(string branchCode, string? keyword, string? type, Guid? groupId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(branchCode))
                    return new Response(Code.BadRequest, "Mã chi nhánh không được để trống!");

                var branchRepo = _unit.GetRepository<Branchs>();
                var branch = await branchRepo.Get(x => x.Code == branchCode && x.Status != 0).FirstOrDefaultAsync();
                if (branch == null)
                    return new Response(Code.NotFound, "Chi nhánh không tồn tại!");

                var menuItemRepo = _unit.GetRepository<MenuItem>();
                var mibRepo = _unit.GetRepository<MenuItemBranch>();

                var hiddenIds = await _visibility.GetHiddenIdsAsync(branchCode);
                var query = from mi in menuItemRepo.Get(x => x.Status == 1 && !hiddenIds.Contains(x.Id))
                            join mib in mibRepo.Get() on mi.Id equals mib.MenuItemId into mibJoin
                            where !mibJoin.Any() || mibJoin.Any(x => x.BranchId == branch.Id)
                            select mi;

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var kw = keyword.Trim().ToLower();
                    query = query.Where(x => (x.Name != null && x.Name.ToLower().Contains(kw))
                                          || (x.Code != null && x.Code.ToLower().Contains(kw))
                                          || (x.Description != null && x.Description.ToLower().Contains(kw)));
                }

                if (!string.IsNullOrWhiteSpace(type))
                {
                    if (Enum.TryParse<MenuGroupType>(type, true, out var grpType))
                    {
                        var groupRepo = _unit.GetRepository<MenuGroup>();
                        var groupIds = await groupRepo.Get(x => x.Type == (int)grpType && x.Status == 1)
                                                       .Select(x => x.Id)
                                                       .ToListAsync();
                        var itemGroupRepo = _unit.GetRepository<MenuItemGroup>();
                        var matchingItemIds = itemGroupRepo.Get(g => groupIds.Contains(g.MenuGroupId)).Select(g => g.MenuItemId);
                        query = query.Where(x => (x.MenuGroupId != null && groupIds.Contains(x.MenuGroupId.Value))
                            || matchingItemIds.Contains(x.Id));
                    }
                }

                if (groupId.HasValue && groupId.Value != Guid.Empty)
                {
                    var selectedGroupId = groupId.Value;
                    var itemGroupRepo = _unit.GetRepository<MenuItemGroup>();
                    var matchingItemIds = itemGroupRepo.Get(g => g.MenuGroupId == selectedGroupId).Select(g => g.MenuItemId);
                    query = query.Where(x => x.MenuGroupId == selectedGroupId
                        || matchingItemIds.Contains(x.Id));
                }

                var list = await query.OrderBy(x => x.CourseOrder).ThenBy(x => x.Name).ToListAsync();

                // Load tax rates and units
                var taxRepo = _unit.GetRepository<TaxRate>();
                var taxes = await taxRepo.Get(x => x.Status == 1).ToListAsync();
                var taxDict = taxes.ToDictionary(t => t.Id, t => (double)t.Rate);

                var unitRepo = _unit.GetRepository<Unit>();
                var units = await unitRepo.Get(x => x.Status != 0).ToListAsync();
                var unitDict = units.ToDictionary(u => u.Id, u => u.Name);

                foreach (var item in list)
                {
                    if (item.TaxRateId.HasValue && taxDict.TryGetValue(item.TaxRateId.Value, out var rate))
                    {
                        item.TaxRatePercent = rate;
                    }
                    if (item.UnitId.HasValue && unitDict.TryGetValue(item.UnitId.Value, out var uName))
                    {
                        item.UnitName = uName;
                    }
                }

                list = list.OrderBy(x => x.CourseOrder > 0 ? x.CourseOrder : 999999).ThenBy(x => x.Name).ToList();

                return new ResponseObject<List<MenuItem>>
                {
                    Code = Code.Success,
                    Message = "Lấy danh sách thực đơn thành công!",
                    Data = list
                };
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("SalesOrderService.GetMenuItemsFilter", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi: " + ex.Message);
            }
        }

        public async Task<Response> GetAvailablePromotions(Guid orderId, Guid? customerId = null, bool overrideCustomer = false)
        {
            try
            {
                var orderRepo = _unit.GetRepository<SalesOrder>();
                var order = await orderRepo.Get(x => x.Id == orderId).FirstOrDefaultAsync();
                if (order == null)
                    return new Response(Code.NotFound, "Không tìm thấy đơn hàng!");

                var now = DateTime.Now;
                var currentDate = now.Date;
                var currentDOW = (int)now.DayOfWeek;
                var currentTime = now.TimeOfDay;

                var promoRepo = _unit.GetRepository<Promotion>();
                var promotions = await promoRepo
                    .Get(x => x.IsActive && x.StartDate <= currentDate && currentDate <= x.EndDate)
                    .ToListAsync();

                var branchRepo = _unit.GetRepository<Branchs>();
                var branch = await branchRepo.Get(x => x.Code == order.BranchCode).FirstOrDefaultAsync();
                var branchId = branch?.Id;

                var effectiveCustomerId = overrideCustomer ? customerId : order.CustomerId;
                var customerGroupId = effectiveCustomerId.HasValue
                    ? (await _unit.GetRepository<BusinessPartner>().Get(x => x.Id == effectiveCustomerId.Value).Select(x => x.PartnerGroupId).FirstOrDefaultAsync())
                    : null;

                var validPromotions = new List<Promotion>();
                foreach (var p in promotions)
                {
                    if (p.ApplyOnlyToQrOrders && !order.IncludesQrOrder) continue;

                    // Check chi nhánh
                    var hasBranch = await _unit.GetRepository<PromotionBranch>().Get(x => x.PromotionId == p.Id).AnyAsync();
                    if (hasBranch && branchId.HasValue)
                    {
                        var matchesBranch = await _unit.GetRepository<PromotionBranch>().Get(x => x.PromotionId == p.Id && x.BranchId == branchId.Value).AnyAsync();
                        if (!matchesBranch) continue;
                    }

                    // Check thứ trong tuần
                    var hasWeekdays = await _unit.GetRepository<PromotionWeekDay>().Get(x => x.PromotionId == p.Id).AnyAsync();
                    if (hasWeekdays)
                    {
                        var matchesWeekday = await _unit.GetRepository<PromotionWeekDay>().Get(x => x.PromotionId == p.Id && x.DayOfWeek == currentDOW).AnyAsync();
                        if (!matchesWeekday) continue;
                    }

                    // Check khung giờ
                    var hasTimeRanges = await _unit.GetRepository<PromotionTimeRange>().Get(x => x.PromotionId == p.Id).AnyAsync();
                    if (hasTimeRanges)
                    {
                        var matchesTime = await _unit.GetRepository<PromotionTimeRange>()
                            .Get(x => x.PromotionId == p.Id && x.StartTime <= currentTime && currentTime <= x.EndTime)
                            .AnyAsync();
                        if (!matchesTime) continue;
                    }

                    // Check nhóm khách hàng
                    var hasCustomerGroups = await _unit.GetRepository<PromotionCustomerGroup>().Get(x => x.PromotionId == p.Id).AnyAsync();
                    if (hasCustomerGroups)
                    {
                        if (!customerGroupId.HasValue) continue;
                        var matchesGroup = await _unit.GetRepository<PromotionCustomerGroup>()
                            .Get(x => x.PromotionId == p.Id && x.CustomerGroupId == customerGroupId.Value)
                            .AnyAsync();
                        if (!matchesGroup) continue;
                    }

                    // Check điều kiện Phiếu trị liệu
                    if (p.RequireTherapyVoucher)
                    {
                        if (string.IsNullOrWhiteSpace(order.TherapyVoucherCode)) continue;
                        var isValidVoucher = await IsTherapyVoucherValidAsync(order.TherapyVoucherCode);
                        if (!isValidVoucher) continue;
                    }

                    validPromotions.Add(p);
                }

                return new ResponseObject<List<Promotion>>
                {
                    Code = Code.Success,
                    Message = "Lấy danh sách khuyến mại khả dụng thành công!",
                    Data = validPromotions
                };
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("SalesOrderService.GetAvailablePromotions", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi: " + ex.Message);
            }
        }

        /// <summary>
        /// Map SalesOrder entity + sub-collections → SalesOrderDto.
        /// </summary>
        private async Task<SalesOrderDto> MapOrderToDtoAsync(
            SalesOrder order,
            IList<SalesOrderItem> items,
            IList<SalesOrderTable> tables,
            IList<SalesOrderPayment> payments,
            IDictionary<Guid, decimal>? fixedTaxDict = null)
        {
            if (fixedTaxDict == null && items != null && items.Any())
            {
                var itemIds = items.Select(x => x.ItemId).Distinct().ToList();
                var menuItemRepo = _unit.GetRepository<MenuItem>();
                fixedTaxDict = menuItemRepo.Get(x => itemIds.Contains(x.Id)).ToDictionary(x => x.Id, x => x.FixedTaxAmount);
            }

            var itemDtos = items?.Select(i =>
            {
                bool isCancelled = i.IsCancelled == 1 || (i.Quantity > 0 && ((i.SoldOutQty + i.CancelledQuantity + i.ReturnedQuantity) >= i.Quantity));
                double billableQty = Math.Max(0, i.Quantity - i.SoldOutQty - i.CancelledQuantity - i.ReturnedQuantity);
                if (i.IsCancelled == 1) billableQty = 0;

                decimal fTax = (fixedTaxDict != null && fixedTaxDict.TryGetValue(i.ItemId, out var tax)) ? tax : 0;
                decimal unitPrice = i.UnitPrice;
                decimal unitVat = fTax > 0
                    ? Math.Round(fTax, 0, MidpointRounding.AwayFromZero)
                    : Math.Round(unitPrice * (decimal)(i.VatRate / 100), 0, MidpointRounding.AwayFromZero);
                decimal unitGross = unitPrice + unitVat;

                decimal effAmount = isCancelled ? 0 : Math.Round((decimal)billableQty * i.UnitPrice, 0, MidpointRounding.AwayFromZero);
                decimal rawDiscountAmount = isCancelled ? 0 : Math.Round(i.DiscountAmount, 0, MidpointRounding.AwayFromZero);
                decimal effDiscountAmount = 0;
                if (!isCancelled && billableQty > 0)
                {
                    if (i.DiscountAmount > 0 && i.Quantity > 0)
                    {
                        // Keep the stored line discount when the whole line is billable.
                        // Rounding a per-unit share here can recreate a few đồng of drift
                        // after CalculateOrderTotal has already corrected the line total.
                        effDiscountAmount = (decimal)billableQty >= (decimal)i.Quantity
                            ? rawDiscountAmount
                            : Math.Min(rawDiscountAmount, Math.Round(rawDiscountAmount * (decimal)billableQty / (decimal)i.Quantity, 0, MidpointRounding.AwayFromZero));
                    }
                    else if (i.DiscountRate > 0)
                    {
                        decimal unitDisc = Math.Round(unitGross * (decimal)(i.DiscountRate / 100), 0, MidpointRounding.AwayFromZero);
                        effDiscountAmount = unitDisc * (decimal)billableQty;
                    }
                }

                decimal effDiscountMoney = isCancelled ? 0 : Math.Max(0, rawDiscountAmount - effDiscountAmount);

                decimal effVatAmount = 0;
                decimal effTotalAmount = 0;
                if (!isCancelled && billableQty > 0)
                {
                    if ((decimal)billableQty >= (decimal)i.Quantity)
                    {
                        // The calculation routine stores the authoritative rounded line
                        // values. Reuse them for a fully billable line instead of
                        // independently rounding the unit discount a second time.
                        effVatAmount = i.VatAmount;
                        effTotalAmount = i.TotalAmount;
                    }
                    else
                    {
                        decimal unitDiscount = billableQty > 0
                            ? effDiscountAmount / (decimal)billableQty
                            : 0;
                        decimal unitTotal = Math.Max(0, unitGross - unitDiscount);
                        if (unitTotal <= 0 || i.DiscountRate >= 100)
                        {
                            effVatAmount = 0;
                            effTotalAmount = 0;
                        }
                        else
                        {
                            effVatAmount = Math.Round(unitVat * (decimal)billableQty, 0, MidpointRounding.AwayFromZero);
                            effTotalAmount = Math.Round(unitTotal * (decimal)billableQty, 0, MidpointRounding.AwayFromZero);
                        }
                    }
                }

                return new SalesOrderItemDto
                {
                    Id                = i.Id,
                    BranchCode        = i.BranchCode,
                    OrderId           = i.OrderId,
                    LineNo            = i.LineNo,
                    ItemId            = i.ItemId,
                    ItemCode          = i.ItemCode,
                    ItemName          = i.ItemName,
                    UnitName          = i.UnitName,
                    Quantity          = i.Quantity,
                    ActualQuantity    = i.ActualQuantity,
                    ReturnedQuantity  = i.ReturnedQuantity,
                    CancelledQuantity = i.CancelledQuantity,
                    UnitPrice         = i.UnitPrice,
                    Amount            = effAmount,
                    DiscountRate      = isCancelled ? 0 : i.DiscountRate,
                    DiscountAmount    = effDiscountAmount,
                    DiscountMoney     = effDiscountMoney,
                    VatRate           = isCancelled ? 0 : i.VatRate,
                    FixedTaxAmount    = isCancelled ? 0 : fTax,
                    VatAmount         = effVatAmount,
                    TotalAmount       = effTotalAmount,
                    IsSentToKitchen   = i.IsSentToKitchen,
                    IsReturned        = i.IsReturned,
                    IsChecked         = i.IsChecked,
                    IsCancelled       = i.IsCancelled,
                    KitchenStatus     = i.KitchenStatus,
                    SentQty           = i.SentQty,
                    ServedQty         = i.ServedQty,
                    SoldOutQty        = i.SoldOutQty,
                    RemainingQty      = i.RemainingQty,
                    PromotionCode     = i.PromotionCode,
                    Note              = i.Note,
                    SelectedOptions = string.IsNullOrWhiteSpace(i.SelectedOptionsJson) ? new() : System.Text.Json.JsonSerializer.Deserialize<List<OrderSelectedOptionDto>>(i.SelectedOptionsJson) ?? new(),
                    CreatedAt         = i.CreatedOnDate,
                    UpdatedAt         = i.UpdatedOnDate
                };
            })?.ToList() ?? new List<SalesOrderItemDto>();

            decimal subTotal = (itemDtos != null && itemDtos.Any()) ? itemDtos.Sum(x => x.Amount) : order.SubTotal;
            decimal discountAmount = (itemDtos != null && itemDtos.Any()) ? itemDtos.Sum(x => x.DiscountAmount) : order.DiscountAmount;
            decimal vatAmount = (itemDtos != null && itemDtos.Any()) ? itemDtos.Sum(x => x.VatAmount) : order.VatAmount;
            decimal totalAmount = (itemDtos != null && itemDtos.Any()) ? itemDtos.Sum(x => x.TotalAmount) : order.TotalAmount;
            if (totalAmount <= 0)
            {
                totalAmount = 0;
                vatAmount = 0;
            }

            int? discountScope = null;

            if (!string.IsNullOrWhiteSpace(order.SelectedPromotionCode))
            {
                var promoRepo = _unit.GetRepository<Promotion>();
                var promo = await promoRepo.Get(x => x.Code == order.SelectedPromotionCode).FirstOrDefaultAsync();
                if (promo != null)
                {
                    var configRepo = _unit.GetRepository<PromotionDiscountConfig>();
                    var config = await configRepo.Get(x => x.PromotionId == promo.Id).FirstOrDefaultAsync();
                    if (config != null)
                    {
                        discountScope = config.DiscountScope;
                    }
                }
            }

            return new SalesOrderDto
            {
                Id               = order.Id,
                BranchCode       = order.BranchCode,
                OrderCode        = order.OrderCode,
                OrderDate        = order.OrderDate,
                OpenTime         = order.OpenTime,
                PaymentTime      = order.PaymentTime,
                CashierUserId    = order.CashierUserId,
                CashierUserName  = order.CashierUserName,
                WaiterId         = order.WaiterId,
                WaiterName       = order.WaiterName,
                CustomerId       = order.CustomerId,
                CustomerCode     = order.CustomerCode,
                CustomerName     = order.CustomerName,
                PhoneNumber      = order.PhoneNumber,
                FloorId          = order.FloorId,
                FloorName        = order.FloorName,
                CustomerCount    = order.CustomerCount,
                ServiceType      = order.ServiceType,
                Status           = order.Status,
                DeliveryAddress  = order.DeliveryAddress,
                DeliveryNote     = order.DeliveryNote,
                ShippingFeeNote  = order.ShippingFeeNote,
                SubTotal         = subTotal,
                DiscountAmount   = discountAmount,
                VatAmount        = vatAmount,
                TotalAmount      = totalAmount,
                SelectedPromotionCode = order.SelectedPromotionCode,
                TherapyVoucherCode = order.TherapyVoucherCode,
                DiscountLabel    = !string.IsNullOrEmpty(order.SelectedPromotionCode) ? ("KM " + order.SelectedPromotionCode) : null,
                DiscountScope    = discountScope,
                CancelReason     = order.CancelReason,
                Note             = order.Note,
                CreatedAt        = order.CreatedOnDate,
                UpdatedAt        = order.UpdatedOnDate,

                Tables = tables.Select(t => new SalesOrderTableDto
                {
                    Id         = t.Id,
                    BranchCode = t.BranchCode,
                    OrderId    = t.OrderId,
                    TableId    = t.TableId,
                    TableCode  = t.TableCode,
                    TableName  = t.TableName,
                    FloorId    = t.FloorId,
                    CreatedAt  = t.CreatedOnDate
                }).ToList(),

                Items = itemDtos,

                Payments = payments.Select(p => new SalesOrderPaymentDto
                {
                    Id                 = p.Id,
                    BranchCode         = p.BranchCode,
                    OrderId            = p.OrderId,
                    TransactionId      = p.TransactionId,
                    PaymentType        = p.PaymentType,
                    PaymentAmount      = p.PaymentAmount,
                    CashReceivedAmount = p.CashReceivedAmount,
                    ChangeAmount       = p.ChangeAmount,
                    CreatedAt          = p.CreatedOnDate
                }).ToList()
            };
        }
        private async Task<string> GenerateOrderCode(string branchCode, DateTime? orderDate = null)
        {
            var targetDate = (orderDate ?? DateTime.Today).Date;
            var prefix = $"{branchCode}{targetDate:yyyyMMdd}-";
            var orderRepo = _unit.GetRepository<SalesOrder>();
            
            var allCodes = await orderRepo.Get(x => x.BranchCode == branchCode 
                                                && x.OrderCode.StartsWith(prefix))
                                          .Select(x => x.OrderCode)
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

            return $"{prefix}{(maxSeq + 1):D4}";
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

        private static string DetermineKitchenStatus(double quantity, double servedQty, double soldOutQty, double cancelledQty, int isSentToKitchen, double sentQty)
        {
            if (quantity <= 0) return "Cancelled";
            if (servedQty >= quantity) return "FullyServed";
            if (servedQty > 0) return "PartiallyServed";
            if (soldOutQty >= quantity) return "FullySoldOut";
            if (soldOutQty > 0) return "PartiallySoldOut";
            if (cancelledQty >= quantity) return "Cancelled";
            if (isSentToKitchen == 1 || sentQty > 0) return "Sent";
            return "NotSent";
        }

        private async Task<bool> IsTherapyVoucherValidAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;

            try
            {
                var documentNumber = code.Trim();
                var baseUrl = _config?["TherapyVoucher:ValidateUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl)) return false;

                string apiUrl;
                if (baseUrl.Contains("{0}"))
                {
                    apiUrl = string.Format(baseUrl, Uri.EscapeDataString(documentNumber));
                }
                else if (baseUrl.Contains("documentNumber="))
                {
                    apiUrl = baseUrl.EndsWith("=") || baseUrl.EndsWith("&") 
                        ? $"{baseUrl}{Uri.EscapeDataString(documentNumber)}" 
                        : $"{baseUrl}&documentNumber={Uri.EscapeDataString(documentNumber)}";
                }
                else
                {
                    apiUrl = $"{baseUrl}?documentNumber={Uri.EscapeDataString(documentNumber)}";
                }

                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);

                var response = await httpClient.GetAsync(apiUrl);
                if (!response.IsSuccessStatusCode) return false;

                var content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(content)) return false;

                using var doc = System.Text.Json.JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    if (root.TryGetProperty("code", out var codeProp) || root.TryGetProperty("Code", out codeProp))
                    {
                        var codeVal = codeProp.GetInt32();
                        if (codeVal != 200) return false;
                    }

                    if (root.TryGetProperty("data", out var dataProp) || root.TryGetProperty("Data", out dataProp))
                    {
                        if (dataProp.ValueKind == System.Text.Json.JsonValueKind.Null) return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static decimal CalculateUnitVat(decimal unitPrice, double vatRate, MenuItem? mi, string? selectedOptionsJson = null)
        {
            decimal optionsTax = 0m;
            decimal optionsCharge = 0m;
            bool parsedOptions = false;

            if (!string.IsNullOrWhiteSpace(selectedOptionsJson))
            {
                try
                {
                    var options = System.Text.Json.JsonSerializer.Deserialize<List<OrderSelectedOptionDto>>(selectedOptionsJson);
                    if (options != null && options.Count > 0)
                    {
                        parsedOptions = true;
                        foreach (var opt in options)
                        {
                            optionsCharge += opt.ExtraCharge;
                            decimal optPercentTax = vatRate > 0
                                ? Math.Round(opt.ExtraCharge * (decimal)(vatRate / 100.0), 0, MidpointRounding.AwayFromZero)
                                : 0m;
                            decimal optFixedTax = opt.FixedTax ?? 0m;
                            optionsTax += optFixedTax != 0 ? optFixedTax : optPercentTax;
                        }
                    }
                }
                catch { }
            }

            if (!parsedOptions && mi != null && unitPrice > mi.SalePrice)
            {
                var extraFromOptions = unitPrice - mi.SalePrice;
                optionsCharge = extraFromOptions;
                if (vatRate > 0)
                {
                    optionsTax += Math.Round(extraFromOptions * (decimal)(vatRate / 100.0), 0, MidpointRounding.AwayFromZero);
                }
            }

            decimal baseItemTax;
            if (mi != null && mi.FixedTaxAmount > 0)
            {
                baseItemTax = Math.Round(mi.FixedTaxAmount, 0, MidpointRounding.AwayFromZero);
            }
            else
            {
                var basePrice = mi != null ? mi.SalePrice : Math.Max(0, unitPrice - optionsCharge);
                baseItemTax = vatRate > 0 ? Math.Round(basePrice * (decimal)(vatRate / 100.0), 0, MidpointRounding.AwayFromZero) : 0m;
            }

            return baseItemTax + optionsTax;
        }
    }
}

