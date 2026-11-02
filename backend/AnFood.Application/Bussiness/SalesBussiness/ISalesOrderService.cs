using System;
using System.Threading.Tasks;
using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.SalesBussiness
{
    /// <summary>
    /// Service nền tảng cho Order/Bill — chịu trách nhiệm:
    ///   - Đọc / ghi order theo BranchCode
    ///   - Validate cross-branch
    ///   - Tính toán tổng tiền
    ///
    /// Mọi method đều nhận BranchCode và validate ngay đầu hàm.
    /// Tuyệt đối không thao tác dữ liệu khác chi nhánh.
    /// </summary>
    public interface ISalesOrderService
    {
        // ── Đọc ─────────────────────────────────────────────────

        /// <summary>
        /// Lấy chi tiết order (gồm items, tables, payments).
        /// Trả 404 nếu không tồn tại. Trả 400 nếu order không thuộc BranchCode.
        /// </summary>
        Task<Response> GetOrderById(Guid orderId, string branchCode);

        /// <summary>
        /// Lấy danh sách orders đang active của một bàn trong BranchCode.
        /// Không trả về các order đã Paid / Completed / Merged / Cancelled.
        /// </summary>
        Task<Response> GetActiveOrdersByTable(Guid tableId, string branchCode);

        // ── Ghi ─────────────────────────────────────────────────

        /// <summary>
        /// Tạo mới hoặc cập nhật order.
        /// - request.OrderId == null → tạo mới
        /// - request.OrderId có giá trị → cập nhật (validate BranchCode trước khi ghi)
        /// </summary>
        Task<Response> SaveOrder(CreateOrUpdateOrderRequest request, string? userCode = null, string? userName = null);

        /// <summary>
        /// Hủy order. Chỉ được hủy order thuộc đúng BranchCode.
        /// </summary>
        Task<Response> CancelOrder(Guid orderId, string branchCode, string reason, string? userCode = null, string? userName = null);

        // ── Tính toán ────────────────────────────────────────────

        /// <summary>
        /// Tính lại tổng tiền order từ các items hiện tại và cập nhật vào SalesOrder.
        /// (SubTotal, DiscountAmount, VatAmount, TotalAmount)
        /// </summary>
        Task<Response> CalculateOrderTotal(Guid orderId, string branchCode);

        /// <summary>
        /// Mở bàn ăn tại chỗ (DineIn). Trả về 409 nếu bàn đã có khách và Force = false.
        /// </summary>
        Task<Response> OpenDineInTable(DineInOpenTableRequest request, string? userCode = null, string? userName = null);

        /// <summary>
        /// Đặt trước bàn ăn (Reserved).
        /// </summary>
        Task<Response> ReserveOrder(DineInOpenTableRequest request);

        /// <summary>
        /// Mở bàn cho đơn đặt trước (chuyển trạng thái từ Reserved sang Serving).
        /// </summary>
        Task<Response> CheckInOrder(Guid orderId, string branchCode);

        /// <summary>
        /// Tạo hóa đơn mang về (TakeAway).
        /// </summary>
        Task<Response> CreateTakeAwayOrder(CreateOrUpdateOrderRequest request);

        /// <summary>
        /// Tạo hóa đơn giao hàng (Delivery).
        /// </summary>
        Task<Response> CreateDeliveryOrder(CreateOrUpdateOrderRequest request);

        /// <summary>
        /// Lấy danh sách orders theo ServiceType (TakeAway hoặc Delivery) và trạng thái hoạt động.
        /// </summary>
        Task<Response> GetOrdersByServiceType(string branchCode, string serviceType, string? statuses);

        /// <summary>
        /// Gửi order/các món chưa gửi cho bếp/bar, đổi trạng thái sang Serving.
        /// </summary>
        Task<Response> SendOrderToKitchenBar(Guid orderId, string branchCode);

        /// <summary>
        /// Chuyển order sang bàn mới.
        /// </summary>
        Task<Response> TransferTable(Guid orderId, List<Guid> targetTableIds, string branchCode, string? note = null);

        /// <summary>
        /// Gộp nhiều order bận vào một order đích.
        /// </summary>
        Task<Response> MergeOrders(Guid targetOrderId, List<Guid> sourceOrderIds, List<Guid>? tableIds, string branchCode);

        /// <summary>
        /// Tách món ăn từ order gốc sang một bàn/hóa đơn khác.
        /// </summary>
        Task<Response> SplitOrder(Guid sourceOrderId, Guid targetTableId, bool isNewBill, List<SplitItemRequest> itemsToSplit, int customerCount, string branchCode);

        /// <summary>
        /// Lấy danh sách kiểm đồ của hóa đơn.
        /// </summary>
        Task<Response> GetCheckItems(Guid orderId, string branchCode);

        /// <summary>
        /// Xác nhận kiểm đồ (cập nhật thực tế sử dụng, hủy/trả lại).
        /// </summary>
        Task<Response> CheckItems(Guid orderId, List<OrderItemCheckRequest> items, string branchCode, string? userCode = null, string? userName = null);

        /// <summary>
        /// Xử lý thanh toán cho hóa đơn (tiền mặt, chuyển khoản, cà thẻ).
        /// </summary>
        Task<Response> ProcessPayment(Guid orderId, string paymentType, decimal paymentAmount, decimal cashReceivedAmount, string? transactionId, string branchCode, string? cashierId = null, string? cashierName = null);

        /// <summary>
        /// Tra cứu danh sách khách hàng của chi nhánh.
        /// </summary>
        Task<Response> GetCustomers(string branchCode, string? keyword);

        /// <summary>
        /// Tra cứu danh sách nhân viên của chi nhánh.
        /// </summary>
        Task<Response> GetBranchEmployees(string branchCode, string? keyword = null);

        /// <summary>
        /// Lấy danh sách 20 món ăn dùng nhiều nhất của tuần trước.
        /// </summary>
        Task<Response> GetTopUsedMenuItems(string branchCode);

        /// <summary>
        /// Bộ lọc danh sách thực đơn (món ăn, đồ uống, combo...).
        /// </summary>
        Task<Response> GetMenuItemsFilter(string branchCode, string? keyword, string? type, Guid? groupId);

        // ── Validate ─────────────────────────────────────────────


        /// <summary>
        /// Kiểm tra orderId có tồn tại và thuộc branchCode không.
        /// Trả false nếu không khớp (dùng để guard trước các thao tác nhạy cảm).
        /// </summary>
        Task<bool> ValidateOrderBranch(Guid orderId, string branchCode);

        /// <summary>
        /// Kiểm tra tableId có tồn tại và thuộc branchCode không.
        /// Trả false nếu không khớp.
        /// </summary>
        Task<bool> ValidateTableBranch(Guid tableId, string branchCode);

        /// <summary>
        /// Kiểm tra sức chứa bàn trước khi tạo/cập nhật order.
        /// </summary>
        Task<Response> ValidateTableCapacityForOrder(string branchCode, System.Collections.Generic.List<Guid> tableIds, int customerCount, System.Collections.Generic.List<Guid>? excludeOrderIds = null);

        /// <summary>
        /// Lấy danh sách khuyến mại khả dụng cho đơn hàng dựa trên điều kiện chi nhánh, thời gian, nhóm khách hàng...
        /// </summary>
        Task<Response> GetAvailablePromotions(Guid orderId, Guid? customerId = null, bool overrideCustomer = false);
    }
}
