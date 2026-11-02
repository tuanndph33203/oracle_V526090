namespace AnFood.Domain.Enums
{
    public enum MenuGroupType
    {
        Food = 1,       // Món ăn
        Beverage = 2,   // Đồ uống
        Combo = 3,      // Combo
        Others = 4      // Khác
    }

    public enum CourseOrder
    {
        Appetizer = 1,  // Món khai vị
        MainCourse = 2, // Món chính
        Dessert = 3     // Món tráng miệng
    }

    public enum MaterialPropertyType
    {
        Ingredient = 1,     // Nguyên liệu
        Goods = 2,          // Hàng hóa
        SemiFinished = 3,   // Bán thành phẩm
        Finished = 4,       // Thành phẩm
        Others = 5          // Khác
    }

    public enum ShelfLifeUnit
    {
        Day = 1,
        Month = 2,
        Year = 3
    }

    public enum ShiftType
    {
        Straight = 1, // Ca thẳng
        Split = 2     // Ca gãy
    }

    public enum PreparationAreaType
    {
        Kitchen = 1, // Bếp
        Bar = 2,     // Bar
        Both = 3     // Cả hai
    }

    public enum PreparationDeviceType
    {
        Tablet = 1, // Máy tính bảng
        Printer = 2, // Máy in
        None = 3     // Không sử dụng
    }

    public enum LabelPrintTiming
    {
        AfterRequest = 1, // Sau khi gửi yêu cầu chế biến
        AfterServe = 2    // Sau khi trả món
    }

    public enum ReasonType
    {
        CancelItem = 1, // Hủy món
        CancelOrder = 2 // Hủy Order
    }

    public enum SaleTableStatus
    {
        Empty = 0,
        WaitingService = 1,
        Serving = 2,
        Reserved = 3,
        WaitingPayment = 4,
        Paid = 5
    }

    /// <summary>
    /// Loại hình phục vụ của order.
    /// </summary>
    public enum ServiceType
    {
        DineIn   = 1, // Tại chỗ
        TakeAway = 2, // Mang về
        Delivery = 3  // Giao hàng
    }

    /// <summary>
    /// Trạng thái vòng đời của order.
    /// Các trạng thái kết thúc (không còn active trên bàn): Paid, Completed, Merged, Cancelled.
    /// </summary>
    public enum OrderStatus
    {
        WaitingService = 1, // Chờ phục vụ
        Reserved       = 2, // Đặt trước
        Serving        = 3, // Đang phục vụ
        WaitingPayment = 4, // Chờ thanh toán
        Payment        = 5, // Đang xử lý thanh toán
        Paid           = 6, // Đã thanh toán
        Completed      = 7, // Hoàn thành
        Merged         = 8, // Đã gộp vào bàn khác
        Cancelled      = 9, // Đã hủy
        Checked        = 10 // Đã kiểm đồ
    }
}
