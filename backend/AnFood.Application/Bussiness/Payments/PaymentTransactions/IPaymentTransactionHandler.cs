using System.Threading;
using System.Threading.Tasks;
using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.Payments.PaymentTransactions
{
    public interface IPaymentTransactionHandler
    {
        Task<Response> CreateTransaction(CreatePaymentTransactionModel model, CancellationToken ct);
        Task<Response> WebhookTransaction(PaymentGatewayWebhookModel payload, CancellationToken ct);
        Task<Response> GetByTransactionId(string transactionId, CancellationToken ct);
        Task<Response> ManualApprove(string transactionId, string approvedByUserCode, string approvedByUserName, CancellationToken ct);
        Task<Response> GetFastCreateHistories(string branchCode, CancellationToken ct);
    }
}
