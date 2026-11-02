using AnFood.Application.Bussiness.Payments.PaymentTransactions;
using AnFood.Application.Helper;
using AnFood.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace AnFood.API.Controllers.Payment
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentTransactionsController : ControllerBase
    {
        private readonly IPaymentTransactionHandler _handler;
        private readonly IConfiguration _config;

        public PaymentTransactionsController(IPaymentTransactionHandler handler, IConfiguration config)
        {
            _handler = handler;
            _config = config;
        }

        [HttpPost]
        [Route("create-transaction")]
        public async Task<Response> CreateTransaction([FromBody] CreatePaymentTransactionModel model, CancellationToken ct)
        {
            var getInfoAccount = User.GetUserClaimsInfo();
            model.CreatedByCode = getInfoAccount.UserCode;
            model.CreatedByUserName = getInfoAccount.Name;

            return await _handler.CreateTransaction(model, ct);
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("webhook")]
        public async Task<Response> Webhook([FromBody] PaymentGatewayWebhookModel payload, CancellationToken ct)
        {
            var expectedKey = _config["PaymentGateway:WebhookApiKey"];
            var providedKey = Request.Headers["X-API-Key"].ToString();

            if (string.IsNullOrWhiteSpace(expectedKey) ||
                string.IsNullOrWhiteSpace(providedKey) ||
                !string.Equals(expectedKey, providedKey, System.StringComparison.Ordinal))
            {
                return new Response(Code.Unauthorized, "Unauthorized");
            }

            return await _handler.WebhookTransaction(payload, ct);
        }

        [HttpGet]
        [Route("status")]
        public async Task<Response> GetStatus([FromQuery] string transactionId, CancellationToken ct)
        {
            return await _handler.GetByTransactionId(transactionId, ct);
        }

        [HttpPut]
        [Route("manual-approve")]
        public async Task<Response> ManualApprove([FromQuery] string transactionId, CancellationToken ct)
        {
            var getInfoAccount = User.GetUserClaimsInfo();
            return await _handler.ManualApprove(transactionId, getInfoAccount.UserCode, getInfoAccount.Name, ct);
        }

        [HttpGet]
        [Route("fast-create-histories")]
        public async Task<Response> GetFastCreateHistories([FromQuery(Name = "BranchCode")] string branchCode, CancellationToken ct)
        {
            return await _handler.GetFastCreateHistories(branchCode, ct);
        }
    }
}
