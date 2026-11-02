using System;
using System.Threading.Tasks;
using AnFood.Application.Bussiness.SalesBussiness;
using AnFood.Infrastructure;
using AnFood.Application.Bussiness.KitchenBussiness;
using AnFood.API.Services;
using AnFood.Application.Services.Mqtt;
using AnFood.Application.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnFood.API.Controllers
{
    [Route("api/sales")]
    [ApiController]
    [Authorize]
    public class SalesController : ControllerBase
    {
        private readonly ISalesHandler      _handler;
        private readonly ISalesOrderService _orderService;
        private readonly IKitchenHandler _kitchenHandler;
        private readonly KitchenPrintDispatcher _printDispatcher;
        private readonly IMqttSpeakerService _mqttSpeakerService;
        private readonly ILogger<SalesController> _logger;

        public SalesController(ISalesHandler handler, ISalesOrderService orderService, IKitchenHandler kitchenHandler, KitchenPrintDispatcher printDispatcher, IMqttSpeakerService mqttSpeakerService, ILogger<SalesController> logger)
        {
            _handler      = handler;
            _orderService = orderService;
            _kitchenHandler = kitchenHandler;
            _printDispatcher = printDispatcher;
            _mqttSpeakerService = mqttSpeakerService;
            _logger = logger;
        }

        [HttpGet("table-map")]
        public async Task<Response> GetTableMap([FromQuery] string branchCode, [FromQuery] Guid floorId, [FromQuery] string status = "All")
        {
            return await _handler.GetTableMap(branchCode, floorId, status);
        }

        [HttpGet("validate-therapy-voucher")]
        public async Task<Response> ValidateTherapyVoucher([FromQuery] string code)
        {
            return await _handler.ValidateTherapyVoucher(code);
        }

        [HttpPost("tables/open")]
        public async Task<Response> OpenTable([FromBody] OpenTableRequest request)
        {
            return await _handler.OpenTable(request);
        }

        [HttpPost("tables/merge/preview")]
        public async Task<Response> MergeTablesPreview([FromBody] MergeTablesRequest request)
        {
            return await _handler.MergeTablesPreview(request);
        }

        [HttpPost("tables/merge")]
        public async Task<Response> MergeTables([FromBody] MergeTablesRequest request)
        {
            return await _handler.MergeTables(request);
        }

        [HttpGet("tables/split/preview")]
        public async Task<Response> SplitTablePreview([FromQuery] Guid tableId, [FromQuery] string branchCode)
        {
            return await _handler.SplitTablePreview(tableId, branchCode);
        }

        [HttpPost("tables/split")]
        public async Task<Response> SplitTable([FromBody] SplitTableRequest request)
        {
            return await _handler.SplitTable(request);
        }

        [HttpPost("tables/cancel")]
        public async Task<Response> CancelTable([FromBody] CancelTableRequest request)
        {
            var response = await _handler.CancelTable(request);
            if (response.Code == Code.Success) _ = _mqttSpeakerService.NotifyBillCancellationAsync();
            return response;
        }

        [HttpPost("tables/transfer")]
        public async Task<Response> TransferTable([FromBody] TransferTableRequest request)
        {
            return await _handler.TransferTable(request);
        }

        [HttpGet("tables/available-targets")]
        public async Task<Response> GetAvailableTargetTables([FromQuery] string branchCode, [FromQuery] Guid? floorId)
        {
            return await _handler.GetAvailableTargetTables(branchCode, floorId);
        }

        [HttpGet("tables/{tableId}/items-check")]
        public async Task<Response> CheckItems([FromRoute] Guid tableId, [FromQuery] string branchCode)
        {
            return await _handler.CheckItems(tableId, branchCode);
        }

        [HttpGet("tables/{tableId}/current-bill")]
        public async Task<Response> GetCurrentBill([FromRoute] Guid tableId, [FromQuery] string branchCode)
        {
            return await _handler.GetCurrentBill(tableId, branchCode);
        }

        [HttpPost("tables/bill/update")]
        public async Task<Response> UpdateBill([FromBody] UpdateBillRequest request)
        {
            return await _handler.UpdateBill(request);
        }

        [HttpPost("tables/open-dine-in")]
        public async Task<Response> OpenDineInTable([FromBody] DineInOpenTableRequest request)
        {
            return await _handler.OpenDineInTable(request);
        }

        [HttpPost("orders/dine-in/save")]
        public async Task<Response> SaveDineInOrder([FromBody] DineInOrderSaveRequest request)
        {
            return await _handler.SaveDineInOrder(request);
        }

        [HttpPost("orders/dine-in/send-kitchen")]
        public async Task<Response> SendDineInOrderToKitchen([FromQuery] Guid orderId, [FromQuery] string branchCode)
        {
            return await _handler.SendDineInOrderToKitchen(orderId, branchCode);
        }

        [HttpPost("orders/dine-in/print-temporary-bill")]
        public async Task<Response> PrintTemporaryBill([FromQuery] Guid orderId, [FromQuery] string branchCode)
        {
            return await _handler.PrintTemporaryBill(orderId, branchCode);
        }

        [HttpPost("orders/dine-in/payment")]
        public async Task<Response> PayDineInOrder([FromBody] DineInPaymentRequest request)
        {
            return await _handler.PayDineInOrder(request);
        }

        [HttpPost("orders/dine-in/cancel")]
        public async Task<Response> CancelDineInOrder([FromBody] CancelDineInOrderRequest request)
        {
            var response = await _handler.CancelDineInOrder(request);
            if (response.Code == Code.Success) _ = _mqttSpeakerService.NotifyBillCancellationAsync();
            return response;
        }

        [HttpGet("menu-items")]
        public async Task<Response> GetMenuItems([FromQuery] string branchCode)
        {
            return await _handler.GetMenuItems(branchCode);
        }

        [HttpPost("orders/history")]
        public async Task<Response> GetOrderHistory([FromBody] OrderHistoryFilterModel model)
        {
            return await _handler.GetOrderHistory(model);
        }

        // ─────────────────────────────────────────────────────────────
        // SALES ORDER FOUNDATION ENDPOINTS  (ISalesOrderService)
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Lấy danh sách order theo ServiceType (TakeAway hoặc Delivery) và trạng thái active.
        /// GET /api/sales/orders/by-service-type?branchCode={branchCode}&serviceType={serviceType}&statuses={statuses}
        /// </summary>
        [HttpGet("orders/by-service-type")]
        public async Task<Response> GetOrdersByServiceType(
            [FromQuery] string branchCode,
            [FromQuery] string serviceType,
            [FromQuery] string? statuses)
        {
            return await _orderService.GetOrdersByServiceType(branchCode, serviceType, statuses);
        }

        /// <summary>
        /// Lấy chi tiết order (gồm items, tables, payments).
        /// BranchCode bắt buộc — không trả dữ liệu khác chi nhánh.
        /// GET /api/sales/orders/{orderId}?branchCode={branchCode}
        /// </summary>
        [HttpGet("orders/{orderId}")]
        public async Task<Response> GetOrderById(
            [FromRoute] Guid orderId,
            [FromQuery] string branchCode)
        {
            return await _orderService.GetOrderById(orderId, branchCode);
        }

        [HttpGet("orders/{orderId}/available-promotions")]
        public async Task<Response> GetAvailablePromotions(
            [FromRoute] Guid orderId,
            [FromQuery] Guid? customerId = null,
            [FromQuery] bool overrideCustomer = false)
        {
            return await _orderService.GetAvailablePromotions(orderId, customerId, overrideCustomer);
        }

        /// <summary>
        /// Lấy danh sách orders đang active của một bàn.
        /// Không trả về orders đã Paid / Completed / Merged / Cancelled.
        /// GET /api/sales/tables/{tableId}/active-orders?branchCode={branchCode}
        /// </summary>
        [HttpGet("tables/{tableId}/active-orders")]
        public async Task<Response> GetActiveOrdersByTable(
            [FromRoute] Guid tableId,
            [FromQuery] string branchCode)
        {
            return await _orderService.GetActiveOrdersByTable(tableId, branchCode);
        }

        /// <summary>
        /// Gộp đơn Chờ xác nhận (QR Order) vào một đơn đang phục vụ hiện tại.
        /// <summary>
        /// Tạo mới hoặc cập nhật order.
        /// - request.OrderId == null  → tạo mới
        /// - request.OrderId có giá trị → cập nhật
        /// POST /api/sales/orders/save
        /// </summary>
        /// <summary>
        /// Tạo mới hoặc cập nhật order.
        /// - request.OrderId == null  → tạo mới
        /// - request.OrderId có giá trị → cập nhật
        /// POST /api/sales/orders/save
        /// </summary>
        [HttpPost("orders/save")]
        public async Task<Response> SaveOrder([FromBody] CreateOrUpdateOrderRequest request)
        {
            var user = User.GetUserClaimsInfo();
            return await _orderService.SaveOrder(request, user?.UserCode, user?.Name);
        }

        /// <summary>
        /// Hủy order. BranchCode + Reason bắt buộc.
        /// POST /api/sales/orders/cancel
        /// </summary>
        [HttpPost("orders/cancel")]
        public async Task<Response> CancelOrder([FromBody] CancelOrderRequest request)
        {
            var user = User.GetUserClaimsInfo();
            var response = await _orderService.CancelOrder(request.OrderId, request.BranchCode, request.Reason, user?.UserCode, user?.Name);
            if (response.Code == Code.Success && response is ResponseObject<OrderActionResultDto> result &&
                result.Data?.WasReserved != true && result.Data?.HadKitchenSentItems == true)
                _ = _mqttSpeakerService.NotifyBillCancellationAsync();
            return response;
        }

        [HttpPost("orders/open-dine-in")]
        public async Task<Response> OpenDineInTableOrder([FromBody] DineInOpenTableRequest request)
        {
            var user = User.GetUserClaimsInfo();
            return await _orderService.OpenDineInTable(request, user?.UserCode, user?.Name);
        }

        [HttpPost("orders/reserve")]
        public async Task<Response> ReserveOrder([FromBody] DineInOpenTableRequest request)
        {
            return await _orderService.ReserveOrder(request);
        }

        [HttpPost("orders/check-in")]
        public async Task<Response> CheckInOrder([FromBody] CheckInOrderRequest request)
        {
            return await _orderService.CheckInOrder(request.OrderId, request.BranchCode);
        }

        [HttpPost("orders/take-away")]
        public async Task<Response> CreateTakeAwayOrder([FromBody] CreateOrUpdateOrderRequest request)
        {
            return await _orderService.CreateTakeAwayOrder(request);
        }

        [HttpPost("orders/delivery")]
        public async Task<Response> CreateDeliveryOrder([FromBody] CreateOrUpdateOrderRequest request)
        {
            return await _orderService.CreateDeliveryOrder(request);
        }

        [HttpPost("orders/send-kitchen-bar")]
        public async Task<Response> SendOrderToKitchenBar([FromQuery] Guid orderId, [FromQuery] string branchCode)
        {
            return await _orderService.SendOrderToKitchenBar(orderId, branchCode);
        }

        [HttpPost("orders/print-kitchen-bar-ticket")]
        public Task<Response> PrintKitchenBarTicket([FromQuery] Guid orderId, [FromQuery] string branchCode)
        {
            return Task.FromResult(new Response(Code.Success, "In phiếu báo bếp/bar thành công!"));
        }

        [HttpPost("orders/transfer-table")]
        public async Task<Response> TransferTableOrder([FromBody] TransferTableOrderRequest request)
        {
            return await _orderService.TransferTable(request.OrderId, request.TargetTableIds, request.BranchCode, request.Note);
        }

        [HttpPost("orders/merge")]
        public async Task<Response> MergeOrders([FromBody] MergeOrdersRequest request)
        {
            return await _orderService.MergeOrders(request.TargetOrderId, request.SourceOrderIds, request.TableIds, request.BranchCode);
        }

        [HttpPost("orders/split")]
        public async Task<Response> SplitOrder([FromBody] SplitOrderRequest request)
        {
            return await _orderService.SplitOrder(request.SourceOrderId, request.TargetTableId, request.IsNewBill, request.ItemsToSplit, request.CustomerCount, request.BranchCode);
        }

        [HttpPost("orders/update-items")]
        public async Task<Response> UpdateItems([FromBody] CreateOrUpdateOrderRequest request)
        {
            var user = User.GetUserClaimsInfo();
            return await _orderService.SaveOrder(request, user?.UserCode, user?.Name);
        }

        [HttpGet("orders/{orderId}/check-items")]
        public async Task<Response> GetCheckItems([FromRoute] Guid orderId, [FromQuery] string branchCode)
        {
            return await _orderService.GetCheckItems(orderId, branchCode);
        }

        [HttpPost("orders/check-items")]
        public async Task<Response> CheckItems([FromBody] CheckItemsPayload request)
        {
            var user = User.GetUserClaimsInfo();
            return await _orderService.CheckItems(request.OrderId, request.Items, request.BranchCode, user?.UserCode, user?.Name);
        }

        [HttpPost("orders/print-check-items")]
        public Task<Response> PrintCheckItems([FromQuery] Guid orderId, [FromQuery] string branchCode)
        {
            return Task.FromResult(new Response(Code.Success, "In phiếu kiểm đồ thành công!"));
        }

        [HttpPost("orders/print-temporary-bill")]
        public async Task<Response> PrintTemporaryBillOrder([FromQuery] Guid orderId, [FromQuery] string branchCode)
        {
            return await _handler.PrintTemporaryBill(orderId, branchCode);
        }

        [HttpPost("orders/payment")]
        public async Task<Response> PayOrder([FromBody] DineInPaymentRequest request)
        {
            var user = User.GetUserClaimsInfo();
            return await _orderService.ProcessPayment(request.OrderId, request.PaymentType, request.PaymentAmount, request.PaymentAmount, request.TransactionId, request.BranchCode, user.UserCode, user.Name);
        }

        [HttpPost("orders/payment/cash")]
        public async Task<Response> PayCash([FromBody] DineInPaymentRequest request)
        {
            var user = User.GetUserClaimsInfo();
            return await _orderService.ProcessPayment(request.OrderId, "Cash", request.PaymentAmount, request.PaymentAmount, request.TransactionId, request.BranchCode, user.UserCode, user.Name);
        }

        [HttpPost("orders/payment/bank-transfer")]
        public async Task<Response> PayBankTransfer([FromBody] DineInPaymentRequest request)
        {
            var user = User.GetUserClaimsInfo();
            return await _orderService.ProcessPayment(request.OrderId, "Transfer", request.PaymentAmount, request.PaymentAmount, request.TransactionId, request.BranchCode, user.UserCode, user.Name);
        }

        [HttpPost("orders/payment/card")]
        public async Task<Response> PayCard([FromBody] DineInPaymentRequest request)
        {
            var user = User.GetUserClaimsInfo();
            return await _orderService.ProcessPayment(request.OrderId, "CreditCard", request.PaymentAmount, request.PaymentAmount, request.TransactionId, request.BranchCode, user.UserCode, user.Name);
        }

        [HttpGet("customers")]
        public async Task<Response> GetCustomers([FromQuery] string branchCode, [FromQuery] string? keyword)
        {
            return await _orderService.GetCustomers(branchCode, keyword);
        }

        [HttpGet("employees")]
        public async Task<Response> GetEmployees([FromQuery] string branchCode, [FromQuery] string? keyword)
        {
            return await _orderService.GetBranchEmployees(branchCode, keyword);
        }

        [HttpGet("menu/top-used")]
        public async Task<Response> GetTopUsedMenuItems([FromQuery] string branchCode)
        {
            return await _orderService.GetTopUsedMenuItems(branchCode);
        }

        [HttpGet("menu/items")]
        public async Task<Response> GetMenuItemsFilter([FromQuery] string branchCode, [FromQuery] string? keyword, [FromQuery] string? type, [FromQuery] Guid? groupId)
        {
            return await _orderService.GetMenuItemsFilter(branchCode, keyword, type, groupId);
        }
    }

    /// <summary>Request body cho POST /api/sales/orders/cancel</summary>
    public class CancelOrderRequest
    {
        public Guid   OrderId    { get; set; }
        public string BranchCode { get; set; } = default!;
        public string Reason     { get; set; } = default!;
    }

    /// <summary>Request body cho POST /api/sales/orders/check-in</summary>
    public class CheckInOrderRequest
    {
        public Guid   OrderId    { get; set; }
        public string BranchCode { get; set; } = default!;
    }

    public class TransferTableOrderRequest
    {
        public Guid OrderId { get; set; }
        public List<Guid> TargetTableIds { get; set; } = new();
        public string BranchCode { get; set; } = default!;
        public string? Note { get; set; }
    }

    public class MergeOrdersRequest
    {
        public Guid TargetOrderId { get; set; }
        public List<Guid> SourceOrderIds { get; set; } = new();
        public List<Guid>? TableIds { get; set; }
        public string BranchCode { get; set; } = default!;
    }

    public class SplitOrderRequest
    {
        public Guid SourceOrderId { get; set; }
        public Guid TargetTableId { get; set; }
        public bool IsNewBill { get; set; }
        public List<SplitItemRequest> ItemsToSplit { get; set; } = new();
        public int CustomerCount { get; set; }
        public string BranchCode { get; set; } = default!;
    }

    public class CheckItemsPayload
    {
        public Guid OrderId { get; set; }
        public string BranchCode { get; set; } = default!;
        public List<OrderItemCheckRequest> Items { get; set; } = new();
    }
}

