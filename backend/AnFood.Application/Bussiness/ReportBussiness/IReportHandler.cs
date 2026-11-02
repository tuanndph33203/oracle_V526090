using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnFood.Infrastructure;
namespace AnFood.Application.Bussiness.ReportBussiness
{
    public interface IReportHandler
    {
        Task<Response> GetRevenueReportByItem(RevenueReportFilterModel filter);
        Task<Response> GetItemInvoiceDetails(Guid itemId, string? branchId, DateTime? fromDate, DateTime? toDate);
        Task<Response> GetDetailedRevenueReport(DetailedReportFilterModel filter);
        Task<Response> GetDailyRevenueReport(DailyRevenueFilterModel filter);
        Task<Response> GetDailyInvoiceDetails(DateTime date, string? branchId);
        Task<Response> GetInvoiceListReport(InvoiceListFilterModel filter);
        Task<Response> GetInvoiceDetailsByCode(string invoiceNo);
        Task<Response> GetDashboardSummary(string? branchCode, DateTime? date);
        Task<byte[]?> ExportExcelAsync(RevenueReportFilterModel filter);
    }
}