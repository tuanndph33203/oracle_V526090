using System;
using System.Text.Json.Serialization;

namespace AnFood.Application.Bussiness.Payments.PaymentTransactions
{
    public class CreatePaymentTransactionModel
    {
        public string? TransactionId { get; set; }
        public string? BranchCode { get; set; }
        public decimal? AmountDue { get; set; }
        public string? CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public bool? IsFastCreate { get; set; }

        public string? CreatedByUserName { get; set; }
        public string? CreatedByCode { get; set; }
        public string? Notes { get; set; }
    }

    public class FastCreatePaymentHistoryDto
    {
        public Guid Id { get; set; }
        public string? TransactionId { get; set; }
        public decimal? AmountDue { get; set; }
        public int? Status { get; set; }
        public string? CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public string? BranchCode { get; set; }
        public DateTime? CreateOnDate { get; set; }
        public string? Notes { get; set; }
    }

    public class CreatePaymentTransactionResult
    {
        [JsonPropertyName("qr_image_url")]
        public string? QrImageUrl { get; set; }

        public decimal? AmountDue { get; set; }

        public string? TransactionId { get; set; }
    }

    public class PaymentGatewayWebhookModel
    {
        [JsonPropertyName("transaction_key")]
        public string? TransactionKey { get; set; }

        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("merchant_transaction_id")]
        public string? MerchantTransactionId { get; set; }

        [JsonPropertyName("status")]
        public int? Status { get; set; }
    }
}
