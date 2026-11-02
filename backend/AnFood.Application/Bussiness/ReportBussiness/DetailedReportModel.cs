using System;
using System.Collections.Generic;

namespace AnFood.Application.Bussiness.ReportBussiness
{
    public class DetailedRevenueDetailDto
    {
        public Guid OrderId { get; set; }
        public string ItemCode { get; set; } = null!;
        public string ItemName { get; set; } = null!;
        public string? UnitName { get; set; }
        public double Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PreTaxAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountPreTaxAmount { get; set; }
        public double TaxRate { get; set; }
        public string? ItemType { get; set; }
        public string? MenuGroup { get; set; }
    }

    public class DetailedRevenueInvoiceDto
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = null!;
        public DateTime OrderDate { get; set; }
        public DateTime OpenTime { get; set; }
        public DateTime? PaymentTime { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PreTaxAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountPreTaxAmount { get; set; }
        public string? CustomerName { get; set; }
        public string? ServerName { get; set; }
        public string? CashierName { get; set; }
        public string? ShiftName { get; set; }
        public string? ServiceType { get; set; }
        public string? FloorName { get; set; }
        public int CustomerCount { get; set; }
        public string? TableName { get; set; }
        public string? Note { get; set; }
        public decimal CashAmount { get; set; }
        public decimal TransferAmount { get; set; }
        public decimal VoucherAmount { get; set; }
        
        public List<DetailedRevenueDetailDto> Details { get; set; } = new();
    }

    public class DetailedRevenueSummaryDto
    {
        public int TotalRecords { get; set; }
        public decimal TotalTotalAmount { get; set; }
        public decimal TotalPreTaxAmount { get; set; }
        public decimal TotalTaxAmount { get; set; }
        public decimal TotalDiscountPreTaxAmount { get; set; }
        public decimal TotalCashAmount { get; set; }
        public decimal TotalTransferAmount { get; set; }
        public decimal TotalVoucherAmount { get; set; }
        public double TotalQuantity { get; set; }
    }

    public class DetailedRevenueResponseDto
    {
        public List<DetailedRevenueInvoiceDto> Invoices { get; set; } = new();
        public DetailedRevenueSummaryDto Summary { get; set; } = new();
    }

    public class DetailedReportFilterModel
    {
        public string? BranchId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
