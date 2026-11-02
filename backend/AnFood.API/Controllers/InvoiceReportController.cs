using System;
using System.Threading.Tasks;
using AnFood.Application.Bussiness.ReportBussiness;
using AnFood.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnFood.API.Controllers
{
    [Route("api")]
    [ApiController]
    [Authorize]
    public class InvoiceReportController : ControllerBase
    {
        private readonly IReportHandler _reportHandler;

        public InvoiceReportController(IReportHandler reportHandler)
        {
            _reportHandler = reportHandler;
        }

        [HttpGet]
        [Route("reports/invoices")]
        public async Task<Response> GetInvoiceListReport([FromQuery] InvoiceListFilterModel filter)
        {
            return await _reportHandler.GetInvoiceListReport(filter);
        }

        [HttpGet]
        [Route("invoices/{invoiceNo}/details")]
        public async Task<Response> GetInvoiceDetails(string invoiceNo)
        {
            return await _reportHandler.GetInvoiceDetailsByCode(invoiceNo);
        }
    }
}