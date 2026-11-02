using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AnFood.Domain.Datatables;
using AnFood.Infrastructure;
using AnFood.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;
using AnFood.Application;

namespace AnFood.Application.Bussiness.Payments.PaymentTransactions
{
    public class PaymentTransactionHandler : IPaymentTransactionHandler
    {
        private readonly IConfiguration _config;
        private readonly IUnitOfWork _unit;

        public PaymentTransactionHandler(IConfiguration config, IUnitOfWork unit)
        {
            _config = config;
            _unit = unit;
        }

        public async Task<Response> CreateTransaction(CreatePaymentTransactionModel model, CancellationToken ct)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.TransactionId))
                    return new Response(Code.BadRequest, "TransactionId is required");
                if (string.IsNullOrWhiteSpace(model.BranchCode))
                    return new Response(Code.BadRequest, "BranchCode is required");
                if (model.AmountDue == null || model.AmountDue <= 0)
                    return new Response(Code.BadRequest, "AmountDue must be greater than 0");

                var baseUrl = _config["PaymentGateway:BaseUrl"];
                var endpoint = _config["PaymentGateway:QrInvoicesEndpoint"];
                var xKey = _config["PaymentGateway:XKey"];

                if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(xKey))
                {
                    return new Response(Code.ServerError, "PaymentGateway configuration is missing (BaseUrl/QrInvoicesEndpoint/XKey).");
                }

                var saleOpenQrValue = await _unit.GetRepository<SystemSetting>()
                    .Get(x => x.Code == Constant.SystemSettingCodes.SaleOpenQr)
                    .AsNoTracking()
                    .Select(x => x.Value)
                    .FirstOrDefaultAsync(ct);

                if (!Constant.SystemSettingAllowsAction(saleOpenQrValue))
                    return new Response(Code.Forbidden, "QR payment is disabled by system setting.");

                var gatewayReq = new GenQrRequest
                {
                    Amount = model.AmountDue.Value,
                    MerchantId = model.BranchCode!,
                    MerchantTransactionId = model.TransactionId!
                };

                GenQrResponse gatewayRes;
                using (var http = new HttpClient { BaseAddress = new Uri(baseUrl) })
                {
                    http.DefaultRequestHeaders.Accept.Clear();
                    http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    http.DefaultRequestHeaders.Add("x-key", xKey);

                    var json = JsonSerializer.Serialize(gatewayReq, JsonOptions);
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");

                    using var resp = await http.PostAsync(endpoint, content, ct);
                    var respText = await resp.Content.ReadAsStringAsync(ct);

                    if (!resp.IsSuccessStatusCode)
                    {
                        Log.Error("PaymentGateway error {StatusCode}: {Body}", (int)resp.StatusCode, respText);
                        return new Response(Code.ServerError, $"PaymentGateway error: {(int)resp.StatusCode}");
                    }

                    var res = JsonSerializer.Deserialize<PaymentGatewayResponse>(respText, JsonOptions);
                    gatewayRes = res?.data!;
                    if (gatewayRes == null || string.IsNullOrWhiteSpace(gatewayRes.TransactionKey))
                        return new Response(Code.ServerError, "PaymentGateway response invalid");
                }

                var entity = new PaymentHistories
                {
                    Id = Guid.NewGuid(),
                    CustomerCode = model.CustomerCode,
                    CustomerName = model.CustomerName,
                    AmountDue = model.AmountDue,
                    TransactionId = model.TransactionId,
                    ExternalTransactionId = gatewayRes.TransactionKey,
                    Status = 1,
                    BranchCode = model.BranchCode,
                    IsFastCreate = model.IsFastCreate,
                    CreatedByUserName = model.CreatedByUserName,
                    CreatedByCode = model.CreatedByCode,
                    CreateOnDate = DateTime.Now,
                    Notes = model.Notes
                };

                _unit.GetRepository<PaymentHistories>().Add(entity);
                if (await _unit.SaveAsync() < 0)
                    return new Response(Code.ServerError, "Save PaymentHistories Error");

                var result = new CreatePaymentTransactionResult
                {
                    QrImageUrl = gatewayRes.QrImageUrl,
                    AmountDue = model.AmountDue,
                    TransactionId = model.TransactionId
                };

                return new ResponseObject<CreatePaymentTransactionResult>(result, "Success", Code.Success);
            }
            catch (OperationCanceledException)
            {
                return new Response(Code.ServerError, "Request cancelled");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CreateTransaction error");
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> WebhookTransaction(PaymentGatewayWebhookModel payload, CancellationToken ct)
        {
            try
            {
                if (payload == null)
                    return new Response(Code.BadRequest, "Payload is required");

                if (string.IsNullOrWhiteSpace(payload.MerchantTransactionId))
                    return new Response(Code.BadRequest, "merchant_transaction_id is required");

                if (payload.Amount == null || payload.Amount <= 0)
                    return new Response(Code.BadRequest, "amount must be greater than 0");

                var repo = _unit.GetRepository<PaymentHistories>();
                var entity = await repo.Get(x => x.TransactionId == payload.MerchantTransactionId)
                    .FirstOrDefaultAsync(ct);

                if (entity == null)
                    return new Response(Code.NotFound, "Transaction not found: " + payload.MerchantTransactionId);

                entity.AmountPaid = payload.Amount;
                entity.Status = 2;
                entity.Type = 1;

                repo.Update(entity);
                await SyncOrderStatusToPaidAsync(entity, ct);
                if (await _unit.SaveAsync() < 0)
                    return new Response(Code.ServerError, "Save PaymentHistories Error");

                return new Response(Code.Success, "Success");
            }
            catch (OperationCanceledException)
            {
                return new Response(Code.ServerError, "Request cancelled");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "WebhookTransaction error");
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetByTransactionId(string transactionId, CancellationToken ct)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(transactionId))
                    return new Response(Code.BadRequest, "TransactionId is required");

                var repo = _unit.GetRepository<PaymentHistories>();
                var entity = await repo.Get(x => x.TransactionId == transactionId)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ct);

                if (entity == null)
                    return new Response(Code.NotFound, "Transaction not found: " + transactionId);

                return new ResponseObject<PaymentHistories>(entity, "Success", Code.Success);
            }
            catch (OperationCanceledException)
            {
                return new Response(Code.ServerError, "Request cancelled");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetByTransactionId error");
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> ManualApprove(string transactionId, string approvedByUserCode, string approvedByUserName, CancellationToken ct)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(transactionId))
                    return new Response(Code.BadRequest, "TransactionId is required");

                var repo = _unit.GetRepository<PaymentHistories>();
                var entity = await repo.Get(x => x.TransactionId == transactionId)
                    .FirstOrDefaultAsync(ct);

                if (entity == null)
                    return new Response(Code.NotFound, "Transaction not found: " + transactionId);

                entity.AmountPaid = entity.AmountDue;
                entity.Status = 2;
                entity.Type = 2;
                entity.ApprovedByUserCode = approvedByUserCode;
                entity.ApprovedByUserName = approvedByUserName;

                repo.Update(entity);
                await SyncOrderStatusToPaidAsync(entity, ct);
                if (await _unit.SaveAsync() < 0)
                    return new Response(Code.ServerError, "Save PaymentHistories Error");

                return new Response(Code.Success, "Success");
            }
            catch (OperationCanceledException)
            {
                return new Response(Code.ServerError, "Request cancelled");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ManualApprove error");
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetFastCreateHistories(string branchCode, CancellationToken ct)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(branchCode))
                    return new Response(Code.BadRequest, "BranchCode is required");

                var repo = _unit.GetRepository<PaymentHistories>();
                var data = await repo.Get(x => x.BranchCode == branchCode && x.IsFastCreate == true && string.IsNullOrEmpty(x.CustomerCode))
                    .AsNoTracking()
                    .OrderByDescending(x => x.CreateOnDate)
                    .Select(x => new FastCreatePaymentHistoryDto
                    {
                        Id = x.Id,
                        TransactionId = x.TransactionId,
                        AmountDue = x.AmountDue,
                        Status = x.Status,
                        CustomerCode = x.CustomerCode,
                        CustomerName = x.CustomerName,
                        BranchCode = x.BranchCode,
                        CreateOnDate = x.CreateOnDate,
                        Notes = x.Notes
                    })
                    .ToListAsync(ct);

                return new ResponseList<FastCreatePaymentHistoryDto>(data, "Success", Code.Success);
            }
            catch (OperationCanceledException)
            {
                return new Response(Code.ServerError, "Request cancelled");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetFastCreateHistories error");
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        private async Task SyncOrderStatusToPaidAsync(PaymentHistories entity, CancellationToken ct)
        {
            if (entity == null || string.IsNullOrWhiteSpace(entity.TransactionId))
                return;

            try
            {
                var transactionId = entity.TransactionId;
                var orderRepo = _unit.GetRepository<SalesOrder>();
                SalesOrder? order = null;

                // 1. Search by OrderCode
                order = await orderRepo.Get(x => x.OrderCode == transactionId).FirstOrDefaultAsync(ct);

                // 2. Search by PAY- style transaction ID
                if (order == null && transactionId.StartsWith("PAY-", StringComparison.OrdinalIgnoreCase))
                {
                    var remaining = transactionId.Substring(4);
                    var lastHyphen = remaining.LastIndexOf('-');
                    if (lastHyphen > 0)
                    {
                        var guidStr = remaining.Substring(0, lastHyphen);
                        if (Guid.TryParse(guidStr, out Guid parsedId))
                        {
                            order = await orderRepo.Get(x => x.Id == parsedId).FirstOrDefaultAsync(ct);
                        }
                    }
                }

                // 3. Search directly by Guid
                if (order == null && Guid.TryParse(transactionId, out Guid directGuid))
                {
                    order = await orderRepo.Get(x => x.Id == directGuid).FirstOrDefaultAsync(ct);
                }

                if (order != null)
                {
                    var oldStatus = order.Status;
                    
                    // Update order status if not already paid/completed
                    if (order.Status != OrderStatusConstants.Paid && order.Status != OrderStatusConstants.Completed)
                    {
                        order.Status = OrderStatusConstants.Paid;
                        order.PaymentTime = DateTime.Now;
                        order.UpdatedOnDate = DateTime.Now;
                        order.UpdatedByUserCode = "system";
                        order.UpdatedByUserName = "System";
                        orderRepo.Update(order);
                        
                        Log.Information("SyncOrderStatusToPaidAsync: Updated order {OrderCode} ({OrderId}) status from {OldStatus} to Paid", 
                            order.OrderCode, order.Id, oldStatus);
                    }

                    // Create SalesOrderPayment
                    var paymentRepo = _unit.GetRepository<SalesOrderPayment>();
                    var paymentAmount = entity.AmountPaid ?? entity.AmountDue ?? order.TotalAmount;
                    
                    var existingPayment = await paymentRepo.Get(x => x.OrderId == order.Id && x.TransactionId == transactionId).AnyAsync(ct);
                    if (!existingPayment)
                    {
                        var payment = new SalesOrderPayment
                        {
                            Id = Guid.NewGuid(),
                            BranchCode = order.BranchCode,
                            OrderId = order.Id,
                            TransactionId = transactionId,
                            PaymentType = "Transfer",
                            PaymentAmount = paymentAmount,
                            CashReceivedAmount = paymentAmount,
                            ChangeAmount = 0,
                            CreatedOnDate = DateTime.Now
                        };
                        paymentRepo.Add(payment);
                        
                        Log.Information("SyncOrderStatusToPaidAsync: Created SalesOrderPayment for order {OrderCode} ({OrderId}), amount: {Amount}", 
                            order.OrderCode, order.Id, paymentAmount);
                    }
                }
                else
                {
                    Log.Warning("SyncOrderStatusToPaidAsync: No matching SalesOrder found for transactionId {TransactionId}", transactionId);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SyncOrderStatusToPaidAsync error for transactionId {TransactionId}", entity.TransactionId);
            }
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private sealed class GenQrRequest
        {
            [JsonPropertyName("amount")]
            public decimal Amount { get; set; }

            [JsonPropertyName("merchant_id")]
            public string MerchantId { get; set; } = default!;

            [JsonPropertyName("merchant_transaction_id")]
            public string MerchantTransactionId { get; set; } = default!;
        }

        public class PaymentGatewayResponse { 
            public bool success { get; set; }
            public string message { get; set; } = default!;
            public GenQrResponse data { get; set; } = default!;
        }

        public sealed class GenQrResponse
        {
            [JsonPropertyName("qr_code")]
            public string QrCode { get; set; } = default!;

            [JsonPropertyName("transaction_key")]
            public string TransactionKey { get; set; } = default!;

            [JsonPropertyName("amount")]
            public decimal Amount { get; set; }

            [JsonPropertyName("qr_image_url")]
            public string QrImageUrl { get; set; } = default!;

            [JsonPropertyName("status")]
            public string Status { get; set; } = default!;

            [JsonPropertyName("merchant_id")]
            public string MerchantId { get; set; } = default!;
        }
    }
}
