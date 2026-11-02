using System;
using System.Collections.Generic;
using System.IO;

namespace AnFood.Infrastructure
{
    public class Constant
    {
        public static class SystemSettingCodes
        {
            public const string SaleOpenQr = "sale-open-QR";
            public const string SaleSendNotion = "sale-send-notion";
            public const string SaleServiceUsedOtp = "sale-service-used-otp";
            public const string QrOrderBaseUrl = "QrOrderBaseUrl";
            public const string EInvoiceLandingUrl = "EINVOICE_LANDING_URL";
            public const string SearchCancelledInvoiceDays = "SearchCancelledInvoiceDays";
            public const string SaleRequireKitchen = "sale-require-kitchen";
            public const string SaleEnableInvoiceDate = "sale-enable-invoice-date";
            public const string SaleEnableKitchenPrint = "sale-enable-kitchen-print";
            public const string KitchenPrintByArea = "kitchen-print-by-area";
            public const string KitchenDefaultAreaId = "kitchen-default-area-id";
            public const string InvoiceLockEnabled = "invoice-lock-enabled";
            public const string InvoiceLockDate = "invoice-lock-date";
            public const string InvoiceLockAuto = "invoice-lock-auto";
            public const string InvoiceLockAutoDays = "invoice-lock-auto-days";
        }

        /// <summary>
        /// Chỉ khi Value là true/1/yes (không phân biệt hoa thường) thì trả về true; null hoặc khác thì false.
        /// </summary>
        public static bool SystemSettingIsExplicitlyTrue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            var v = value.Trim();
            return v.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                   v.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                   v.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// SystemSetting.Value: null/empty hoặc không phải false rõ ràng thì cho phép; false/0/no thì chặn.
        /// </summary>
        public static bool SystemSettingAllowsAction(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true;
            var v = value.Trim();
            if (v.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                v.Equals("0", StringComparison.OrdinalIgnoreCase) ||
                v.Equals("no", StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }
    }

    /// <summary>
    /// Hằng số chuỗi cho trạng thái Order (khớp với giá trị lưu DB).
    /// Dùng thay vì hard-code string để tránh typo và dễ refactor.
    /// </summary>
    public static class OrderStatusConstants
    {
        public const string WaitingService = "WaitingService"; // Chờ phục vụ
        public const string Reserved       = "Reserved";       // Đặt trước
        public const string Serving        = "Serving";        // Đang phục vụ
        public const string Checked        = "Checked";        // Đã kiểm đồ
        public const string WaitingPayment = "WaitingPayment"; // Chờ thanh toán
        public const string Payment        = "Payment";        // Đang xử lý thanh toán
        public const string Paid           = "Paid";           // Đã thanh toán
        public const string Completed      = "Completed";      // Hoàn thành
        public const string Merged         = "Merged";         // Đã gộp
        public const string Cancelled         = "Cancelled";         // Đã hủy
        public const string ApprovedCancelled = "ApprovedCancelled"; // Đã duyệt hủy
        public const string WaitingConfirm    = "WaitingConfirm";    // Chờ xác nhận

        /// <summary>
        /// Các trạng thái kết thúc — order KHÔNG còn active trên bàn.
        /// </summary>
        public static readonly string[] InactiveStatuses = { Paid, Completed, Cancelled, Merged, ApprovedCancelled };

        /// <summary>
        /// Kiểm tra order có đang active (chưa kết thúc) không.
        /// </summary>
        public static bool IsActive(string status) =>
            !InactiveStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Kiểm tra xem sự chuyển đổi trạng thái từ currentStatus sang nextStatus có hợp lệ đối với hành động action hay không.
        /// </summary>
        public static bool CanTransition(string? currentStatus, string nextStatus, string action)
        {
            string current = string.IsNullOrWhiteSpace(currentStatus) ? "Draft" : currentStatus;

            if (action == "Save")
            {
                if (current == "Draft")
                {
                    return nextStatus == Reserved || nextStatus == Serving || nextStatus == WaitingPayment;
                }
                return nextStatus == current || (current == Reserved && nextStatus == Serving) || (current == Serving && nextStatus == WaitingPayment) || (current == Checked && nextStatus == WaitingPayment) || (current == WaitingPayment && nextStatus == Serving) || (current == Checked && nextStatus == Serving);
            }

            if (action == "Reserve")
            {
                if (current == "Draft" || current == WaitingService)
                {
                    return nextStatus == Reserved;
                }
                return false;
            }

            if (action == "CheckIn" || action == "OpenTable")
            {
                if (current == Reserved || current == WaitingService || current == "Draft")
                {
                    return nextStatus == Serving;
                }
                return false;
            }

            if (action == "SendKitchenBar")
            {
                if (current == "Draft" || current == Reserved || current == WaitingConfirm || current == WaitingService || current == Serving || current == WaitingPayment || current == Checked)
                {
                    return nextStatus == Serving;
                }
                return false;
            }

            if (action == "PrintTemporaryBill")
            {
                if (current == WaitingService || current == Serving || current == Checked || current == WaitingPayment)
                {
                    return nextStatus == WaitingPayment;
                }
                return false;
            }

            if (action == "Payment")
            {
                if (current == WaitingPayment || current == Serving || current == WaitingService || current == Reserved || current == Checked)
                {
                    return nextStatus == Paid || nextStatus == Completed;
                }
                return false;
            }

            if (action == "Merge")
            {
                if (current == WaitingConfirm)
                {
                    return nextStatus == Merged;
                }
                return false;
            }

            if (action == "Cancel")
            {
                if (current == Reserved || current == WaitingConfirm || current == WaitingService || current == Serving || current == WaitingPayment || current == Checked)
                {
                    return nextStatus == Cancelled;
                }
                return false;
            }

            return false;
        }
    }

    /// <summary>
    /// Hằng số chuỗi cho loại hình phục vụ (khớp với giá trị lưu DB).
    /// </summary>
    public static class ServiceTypeConstants
    {
        public const string DineIn   = "DineIn";   // Tại chỗ
        public const string TakeAway = "TakeAway"; // Mang về
        public const string Delivery = "Delivery"; // Giao hàng
    }
}