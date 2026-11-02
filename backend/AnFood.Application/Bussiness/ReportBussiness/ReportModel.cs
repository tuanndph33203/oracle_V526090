using System;
using System.Collections.Generic;

namespace AnFood.Application.Bussiness.ReportBussiness
{
    public class RevenueReportItemDto
    {
        public long STT { get; set; }
        public Guid ItemId { get; set; }
        public string ItemCode { get; set; } = null!;
        public string ItemName { get; set; } = null!;
        public string? UnitName { get; set; }
        public double Quantity { get; set; }
        public decimal AvgPrice { get; set; }
        public decimal Total { get; set; }
        public decimal Revenue { get; set; }
        public decimal ItemAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? ItemType { get; set; }
        public string? MenuGroup { get; set; }
    }

    public class RevenueReportSummaryDto
    {
        public int TotalRecords { get; set; }
        public double TotalQuantity { get; set; }
        public decimal TotalTotal { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalItemAmount { get; set; }
        public decimal TotalDiscountAmount { get; set; }
    }

    public class RevenueReportResponseDto
    {
        public List<RevenueReportItemDto> Items { get; set; } = new();
        public RevenueReportSummaryDto Summary { get; set; } = new();
    }

    public class ItemInvoiceDetailDto
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = null!;
        public DateTime OrderDate { get; set; }
        public string? TableName { get; set; }
        public double Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class RevenueReportFilterModel
    {
        public string? BranchId { get; set; }
        public int ReportType { get; set; } = 2; // 2: By Item
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class DailyRevenueFilterModel
    {
        public string? BranchId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class DailyRevenueItemDto
    {
        public long STT { get; set; }
        public DateTime ReportDate { get; set; }
        public decimal PreTaxAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal CashAmount { get; set; }
        public decimal TransferAmount { get; set; }
        public decimal VoucherAmount { get; set; }
        public decimal ActualReceipt { get; set; }
        public int CustomerCount { get; set; }
    }

    public class DailyRevenueSummaryDto
    {
        public int TotalRecords { get; set; }
        public decimal TotalPreTaxAmount { get; set; }
        public decimal TotalTaxAmount { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public decimal TotalTotalAmount { get; set; }
        public decimal TotalCashAmount { get; set; }
        public decimal TotalTransferAmount { get; set; }
        public decimal TotalVoucherAmount { get; set; }
        public decimal TotalActualReceipt { get; set; }
        public int TotalCustomerCount { get; set; }
    }

    public class DailyRevenueResponseDto
    {
        public List<DailyRevenueItemDto> Items { get; set; } = new();
        public DailyRevenueSummaryDto Summary { get; set; } = new();
    }

    public class DailyInvoiceDetailDto
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = null!;
        public string? TableName { get; set; }
        public string? ServiceType { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Status { get; set; }
        public DateTime OrderDate { get; set; }
        public string? CashierUserName { get; set; }
    }

    public class InvoiceListFilterModel
    {
        public string? BranchId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class InvoiceListItemDto
    {
        public Guid OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? OpenTime { get; set; }
        public DateTime? PaymentTime { get; set; }
        public string OrderCode { get; set; } = null!;
        public decimal PreTaxAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? CustomerInfo { get; set; }
        public int CustomerCount { get; set; }
        public string? ServiceType { get; set; }
        public string? AreaName { get; set; }
        public decimal CashAmount { get; set; }
        public decimal TransferAmount { get; set; }
        public decimal VoucherAmount { get; set; }
        public string? TableInfo { get; set; }
        public string? CashierName { get; set; }
        public string? DeliveryPartner { get; set; }
        public string? WaiterName { get; set; }
        public string? Note { get; set; }
        public string? PromotionInfo { get; set; }
        public string? RatingContent { get; set; }
    }

    public class InvoiceListSummaryDto
    {
        public int TotalRecords { get; set; }
        public decimal TotalPreTaxAmount { get; set; }
        public decimal TotalTaxAmount { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public decimal TotalTotalAmount { get; set; }
        public decimal TotalCashAmount { get; set; }
        public decimal TotalTransferAmount { get; set; }
        public decimal TotalVoucherAmount { get; set; }
        public int TotalCustomerCount { get; set; }
    }

    public class InvoiceListResponseDto
    {
        public List<InvoiceListItemDto> Items { get; set; } = new();
        public InvoiceListSummaryDto Summary { get; set; } = new();
    }

    public class InvoiceDetailPopupDto
    {
        public string OrderCode { get; set; } = null!;
        public DateTime OrderDate { get; set; }
        public string? TableNames { get; set; }
        public string? ServiceType { get; set; }
        public string? ShippingFeeNote { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? CashierName { get; set; }
        public string? Note { get; set; }
        public List<InvoiceDetailItemDto> Items { get; set; } = new();
        public List<InvoiceDetailPaymentDto> Payments { get; set; } = new();
    }

    public class InvoiceDetailItemDto
    {
        public string ItemName { get; set; } = null!;
        public double Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public List<AnFood.Application.Bussiness.SalesBussiness.OrderSelectedOptionDto> SelectedOptions { get; set; } = new();
    }

    public class InvoiceDetailPaymentDto
    {
        public string PaymentType { get; set; } = null!;
        public decimal PaymentAmount { get; set; }
    }

    public class DashboardSummaryDto
    {
        public decimal TodayRevenue { get; set; }
        public decimal YesterdayRevenue { get; set; }
        public int TodayOrderCount { get; set; }
        public int ServingOrderCount { get; set; }
        public int WaitingPaymentCount { get; set; }
        public int OccupiedTables { get; set; }
        public int TotalTables { get; set; }
        public int QrOrderCount { get; set; }
        public List<HourlySalesDto> HourlySales { get; set; } = new();
        public List<PaymentMethodSummaryDto> PaymentMethods { get; set; } = new();
        public List<DashboardRecentTransactionDto> RecentTransactions { get; set; } = new();
        public List<DashboardTopMenuItemDto> TopMenuItems { get; set; } = new();
    }

    public class HourlySalesDto
    {
        public string HourLabel { get; set; } = string.Empty;
        public decimal RevenueToday { get; set; }
        public decimal RevenueYesterday { get; set; }
    }

    public class PaymentMethodSummaryDto
    {
        public string MethodName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public double Percentage { get; set; }
    }

    public class DashboardRecentTransactionDto
    {
        public string Id { get; set; } = string.Empty;
        public string OrderCode { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class DashboardTopMenuItemDto
    {
        public int Rank { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public int SoldQty { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
