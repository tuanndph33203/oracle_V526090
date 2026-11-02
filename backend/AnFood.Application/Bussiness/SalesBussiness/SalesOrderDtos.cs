using System;
using System.Collections.Generic;

namespace AnFood.Application.Bussiness.SalesBussiness
{
    // ────────────────────────────────────────────────────────────
    // SNAPSHOT DTOs — ánh xạ 1-1 từ entity, dùng để trả về client
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// Full snapshot của SalesOrder — dùng cho màn chi tiết order / bill.
    /// </summary>
    public class SalesOrderDto
    {
        public Guid   Id               { get; set; }
        public string BranchCode       { get; set; } = default!;
        public string OrderCode        { get; set; } = default!;
        public DateTime OrderDate      { get; set; }
        public DateTime OpenTime       { get; set; }
        public DateTime? PaymentTime   { get; set; }

        // Nhân viên
        public string? CashierUserId   { get; set; }
        public string? CashierUserName { get; set; }
        public Guid?   WaiterId        { get; set; }
        public string? WaiterName      { get; set; }

        // Khách hàng
        public Guid?   CustomerId      { get; set; }
        public string? CustomerCode    { get; set; }
        public string? CustomerName    { get; set; }
        public string? PhoneNumber     { get; set; }

        // Vị trí
        public Guid    FloorId         { get; set; }
        public string? FloorName       { get; set; }
        public int     CustomerCount   { get; set; }
        public Guid?   TableId         => Tables?.FirstOrDefault()?.TableId;
        public string? TableCode       => Tables?.FirstOrDefault()?.TableCode;
        public string? TableName       => (Tables != null && Tables.Any()) ? string.Join(" + ", Tables.Select(t => t.TableName).Where(n => !string.IsNullOrEmpty(n))) : null;
        public string? TableNames      => TableName;
        public string? TableInfo       => TableName;

        // Loại hình & trạng thái
        public string ServiceType      { get; set; } = default!; // DineIn / TakeAway / Delivery
        public string Status           { get; set; } = default!;

        // Giao hàng
        public string? DeliveryAddress { get; set; }
        public string? DeliveryNote    { get; set; }
        public string? ShippingFeeNote { get; set; }

        // Tài chính
        public decimal SubTotal        { get; set; }
        public decimal DiscountAmount  { get; set; }
        public decimal VatAmount       { get; set; }
        public decimal TotalAmount     { get; set; }
        public string? SelectedPromotionCode { get; set; }
        public string? TherapyVoucherCode { get; set; }
        public string? DiscountLabel   { get; set; }
        public int?    DiscountScope   { get; set; }


        // Ghi chú & hủy
        public string? CancelReason    { get; set; }
        public string? Note            { get; set; }

        // Audit
        public DateTime? CreatedAt     { get; set; }
        public DateTime? UpdatedAt     { get; set; }

        // Sub-collections
        public List<SalesOrderTableDto>   Tables   { get; set; } = new();
        public List<SalesOrderItemDto>    Items    { get; set; } = new();
        public List<SalesOrderPaymentDto> Payments { get; set; } = new();
    }

    /// <summary>
    /// Snapshot của một dòng món ăn trong order.
    /// </summary>
    public class SalesOrderItemDto
    {
        public Guid   Id                { get; set; }
        public string BranchCode        { get; set; } = default!;
        public Guid   OrderId           { get; set; }
        public int    LineNo            { get; set; }

        // Món
        public Guid   ItemId            { get; set; }
        public string ItemCode          { get; set; } = default!;
        public string ItemName          { get; set; } = default!;
        public string? UnitName         { get; set; }

        // Số lượng
        public double Quantity          { get; set; }
        public double ActualQuantity    { get; set; }
        public double ReturnedQuantity  { get; set; }
        public double CancelledQuantity { get; set; }

        // Giá
        public decimal UnitPrice        { get; set; }
        public decimal Amount           { get; set; }
        public double  DiscountRate     { get; set; }
        public decimal DiscountAmount   { get; set; }
        public decimal? DiscountMoney   { get; set; }
        public double  VatRate          { get; set; }
        public decimal FixedTaxAmount   { get; set; }
        public decimal VatAmount        { get; set; }
        public decimal TotalAmount      { get; set; }

        // Cờ trạng thái
        public int IsSentToKitchen  { get; set; } // 0=chưa gửi, 1=đã gửi
        public int IsReturned       { get; set; } // 0=không trả, 1=trả lại
        public int IsChecked        { get; set; } // 0=chưa kiểm, 1=đã kiểm
        public int IsCancelled      { get; set; } // 0=không hủy, 1=đã hủy

        public string KitchenStatus { get; set; } = "NotSent";
        public double SentQty { get; set; } = 0;
        public double ServedQty { get; set; } = 0;
        public double SoldOutQty { get; set; } = 0;
        public double RemainingQty { get; set; } = 0;

        public string? PromotionCode { get; set; }
        public string? Note          { get; set; }
        public List<OrderSelectedOptionDto> SelectedOptions { get; set; } = new();
        public DateTime? CreatedAt   { get; set; }
        public DateTime? UpdatedAt   { get; set; }
    }

    /// <summary>
    /// Snapshot liên kết order ↔ bàn.
    /// </summary>
    public class SalesOrderTableDto
    {
        public Guid     Id          { get; set; }
        public string   BranchCode  { get; set; } = default!;
        public Guid     OrderId     { get; set; }
        public Guid     TableId     { get; set; }
        public string   TableCode   { get; set; } = default!;
        public string   TableName   { get; set; } = default!;
        public Guid     FloorId     { get; set; }
        public DateTime CreatedAt   { get; set; }
    }

    /// <summary>
    /// Snapshot một lần thanh toán.
    /// </summary>
    public class SalesOrderPaymentDto
    {
        public Guid     Id                  { get; set; }
        public string   BranchCode          { get; set; } = default!;
        public Guid     OrderId             { get; set; }
        public string?  TransactionId       { get; set; }
        public string   PaymentType         { get; set; } = default!;
        public decimal  PaymentAmount       { get; set; }
        public decimal  CashReceivedAmount  { get; set; }
        public decimal  ChangeAmount        { get; set; }
        public DateTime CreatedAt           { get; set; }
    }

    // ────────────────────────────────────────────────────────────
    // REQUEST MODELS
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// Request tạo mới hoặc cập nhật order.
    /// Nếu OrderId == null → tạo mới. Nếu có OrderId → cập nhật.
    /// </summary>
    public class CreateOrUpdateOrderRequest
    {
        // Identity
        public Guid?  OrderId        { get; set; }   // null = tạo mới
        public string BranchCode     { get; set; } = default!;
        public DateTime? OpenTime    { get; set; }   // Giờ vào của đơn hàng
        public DateTime? OrderDate   { get; set; }   // Ngày order

        // Bàn (có thể nhiều bàn, bỏ qua nếu TakeAway / Delivery)
        public List<Guid> TableIds   { get; set; } = new();

        // Khách hàng
        public Guid?   CustomerId    { get; set; }
        public bool    CustomerIdSpecified { get; set; }
        public string? CustomerCode  { get; set; }
        public string? CustomerName  { get; set; }
        public string? PhoneNumber   { get; set; }
        public int     CustomerCount { get; set; } = 1;

        // Nhân viên
        public Guid?   WaiterId      { get; set; }
        public string? WaiterName    { get; set; }

        // Loại hình
        public string  ServiceType   { get; set; } = "DineIn"; // DineIn / TakeAway / Delivery

        // Trạng thái (để chỉ định trạng thái mong muốn khi lưu)
        public string? Status        { get; set; }

        // Giao hàng
        public string? DeliveryAddress { get; set; }
        public string? DeliveryNote    { get; set; }
        public string? ShippingFeeNote { get; set; }

        // Ghi chú chung
        public string? Note          { get; set; }

        public string? SelectedPromotionCode { get; set; }
        public string? TherapyVoucherCode { get; set; }

        // Danh sách món
        public List<OrderItemRequest> Items { get; set; } = new();
    }

    /// <summary>
    /// Một dòng món trong CreateOrUpdateOrderRequest.
    /// </summary>
    public class OrderItemRequest
    {
        public Guid?   Id             { get; set; }   // null = thêm mới, có = cập nhật
        public Guid    ItemId         { get; set; }
        public string  ItemCode       { get; set; } = default!;
        public string  ItemName       { get; set; } = default!;
        public string? UnitName       { get; set; }
        public double  Quantity       { get; set; }
        public decimal UnitPrice      { get; set; }
        public double  DiscountRate   { get; set; }
        public double  VatRate        { get; set; }
        public decimal FixedTaxAmount { get; set; }
        public string? PromotionCode  { get; set; }
        public string? Note           { get; set; }
        public List<OrderSelectedOptionRequest>? SelectedOptions { get; set; }
        internal string? SelectedOptionsJson { get; set; }
    }

    public class OrderSelectedOptionRequest
    {
        public string GroupCode { get; set; } = default!;
        public string ValueCode { get; set; } = default!;
    }

    public class OrderSelectedOptionDto : OrderSelectedOptionRequest
    {
        public string GroupName { get; set; } = default!;
        public string ValueName { get; set; } = default!;
        public decimal ExtraCharge { get; set; }
        public decimal? FixedTax { get; set; }
    }

    // ────────────────────────────────────────────────────────────
    // RESULT MODELS
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// Kết quả trả về sau một action (tạo, cập nhật, hủy, …).
    /// </summary>
    public class OrderActionResultDto
    {
        public Guid   OrderId    { get; set; }
        public string OrderCode  { get; set; } = default!;
        public string Status     { get; set; } = default!;
        public string Message    { get; set; } = default!;
        public bool   Success    { get; set; }
        public bool   WasReserved { get; set; }
        public bool   HadKitchenSentItems { get; set; }
        public List<AnFood.Application.Bussiness.KitchenBussiness.KitchenPrintSlipDto> PrintSlips { get; set; } = new();
        public List<AnFood.Application.Bussiness.KitchenBussiness.KitchenPrintDispatchResultDto> PrintDispatchResults { get; set; } = new();
    }

    /// <summary>
    /// Thông tin rút gọn của một order đang active — dùng cho table map / danh sách bàn.
    /// </summary>
    public class ActiveOrderDto
    {
        public Guid     OrderId       { get; set; }
        public string   OrderCode     { get; set; } = default!;
        public string   Status        { get; set; } = default!;
        public string   ServiceType   { get; set; } = default!;
        public DateTime OpenTime      { get; set; }
        public int      CustomerCount { get; set; }
        public Guid?    WaiterId      { get; set; }
        public string?  WaiterName    { get; set; }
        public decimal  TotalAmount   { get; set; }
        public int      ItemCount     { get; set; }  // tổng số dòng món
        public int      UnsentItems   { get; set; }  // số dòng chưa gửi bếp
    }

    /// <summary>
    /// Danh sách orders đang active của một bàn.
    /// </summary>
    public class TableActiveOrdersDto
    {
        public Guid               TableId    { get; set; }
        public string             TableCode  { get; set; } = default!;
        public string             TableName  { get; set; } = default!;
        public string             BranchCode { get; set; } = default!;
        public int                Capacity   { get; set; }
        public int                ActiveOrderCount { get; set; }
        public int                ActiveCustomerCount { get; set; }
        public int                RemainingCapacity { get; set; }
        public List<ActiveOrderDto> Orders   { get; set; } = new();
    }

    /// <summary>
    /// Yêu cầu kiểm từng dòng món ăn
    /// </summary>
    public class OrderItemCheckRequest
    {
        public Guid ItemId { get; set; }
        public double CheckedQuantity { get; set; }
        public string? Note { get; set; }
    }

    /// <summary>
    /// Yêu cầu kiểm đồ cho cả order
    /// </summary>
    public class CheckItemsRequest
    {
        public Guid OrderId { get; set; }
        public string BranchCode { get; set; } = default!;
        public List<OrderItemCheckRequest> Items { get; set; } = new();
    }

    public class OrdersByServiceTypeDto
    {
        public string BranchCode { get; set; } = default!;
        public string ServiceType { get; set; } = default!;
        public List<ServiceTypeOrderDto> Orders { get; set; } = new();
    }

    public class ServiceTypeOrderDto
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = default!;
        public string ServiceType { get; set; } = default!;
        public string Status { get; set; } = default!;
        public Guid? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? DeliveryAddress { get; set; }
        public string? DeliveryNote { get; set; }
        public string? Note { get; set; }
        public DateTime OpenTime { get; set; }
        public decimal TotalAmount { get; set; }
        public string? CashierUserName { get; set; }
        public string? WaiterName { get; set; }
        public bool HasUnsentKitchenItems { get; set; }
        public string? TableName { get; set; }
        public Guid? FloorId { get; set; }
        public string? FloorName { get; set; }
        public Guid? TableId { get; set; }
        public List<ActiveOrderMapInfo> ActiveOrders { get; set; } = new();
    }

    public class SalesRealtimeEventDto
    {
        /// <summary>Type: OrderConfirmed | QrOrderMerged | TableStatusChanged | OrderPaid | OrderCancelled | TableOpened | TableTransferred | TablesMerged | OrderUpdated</summary>
        public string EventType { get; set; } = default!;
        public Guid? OrderId { get; set; }
        public string? OrderCode { get; set; }
        public Guid? TableId { get; set; }
        public string? TableName { get; set; }
        public string? NewStatus { get; set; }
        public string? ServiceType { get; set; }
        public string? ActionBy { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}

