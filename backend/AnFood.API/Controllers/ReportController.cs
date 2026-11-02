using System;
using System.Threading.Tasks;
using AnFood.Application.Bussiness.ReportBussiness;
using AnFood.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnFood.API.Controllers
{
    [Route("api/reports")]
    [ApiController]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly IReportHandler _reportHandler;

        public ReportController(IReportHandler reportHandler)
        {
            _reportHandler = reportHandler;
        }

        [HttpGet]
        [Route("revenue-by-item")]
        public async Task<Response> GetRevenueReportByItem([FromQuery] RevenueReportFilterModel filter)
        {
            return await _reportHandler.GetRevenueReportByItem(filter);
        }

        [HttpGet]
        [Route("detailed-revenue")]
        public async Task<Response> GetDetailedRevenueReport([FromQuery] DetailedReportFilterModel filter)
        {
            return await _reportHandler.GetDetailedRevenueReport(filter);
        }

        [HttpGet]
        [Route("revenue-by-item/{itemId}/invoices")]
        public async Task<Response> GetItemInvoiceDetails(
            Guid itemId, 
            [FromQuery] string? branchId, 
            [FromQuery] DateTime? fromDate, 
            [FromQuery] DateTime? toDate)
        {
            return await _reportHandler.GetItemInvoiceDetails(itemId, branchId, fromDate, toDate);
        }

        [HttpGet]
        [Route("daily-revenue")]
        public async Task<Response> GetDailyRevenue([FromQuery] DailyRevenueFilterModel filter)
        {
            return await _reportHandler.GetDailyRevenueReport(filter);
        }

        [HttpGet]
        [Route("daily-revenue/{date}/invoices")]
        public async Task<Response> GetDailyInvoiceDetails(
            DateTime date, 
            [FromQuery] string? branchId)
        {
            return await _reportHandler.GetDailyInvoiceDetails(date, branchId);
        }

        [HttpGet]
        [Route("dashboard-summary")]
        public async Task<Response> GetDashboardSummary(
            [FromQuery] string? branchCode,
            [FromQuery] DateTime? date)
        {
            return await _reportHandler.GetDashboardSummary(branchCode, date);
        }

        [HttpPost]
        [Route("sync-data")]
        public async Task<Response> SyncData()
        {
            // Mock trigger sync data
            await Task.Delay(500); // Simulate API latency
            return new Response(Code.Success, "Đồng bộ dữ liệu từ máy bán hàng thành công!");
        }

        [HttpGet]
        [Route("export-excel")]
        public async Task<IActionResult> ExportExcel([FromQuery] RevenueReportFilterModel filter)
        {
            var bytes = await _reportHandler.ExportExcelAsync(filter);
            if (bytes == null || bytes.Length == 0)
            {
                return BadRequest("Không thể xuất file Excel báo cáo hoặc dữ liệu rỗng.");
            }

            string baseName = filter.ReportType switch
            {
                1 => "BaoCaoDoanhThuTheoNgay",
                2 => "BaoCaoDoanhThuMatHang",
                3 => "BangKeHoaDon",
                4 => "BaoCaoChiTietDoanhThu",
                _ => "BaoCaoDoanhThu"
            };

            var fileName = $"{baseName}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
