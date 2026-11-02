using AnFood.Application.Bussiness.Payments.PaymentTransactions;
using AnFood.Domain.Datatables;
using AnFood.API.Hubs;
using AnFood.Infrastructure;
using AnFood.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AnFood.API.Handlers
{
    public class PaymentTransactionHandlerRealtime : IPaymentTransactionHandler
    {
        private readonly PaymentTransactionHandler _inner;
        private readonly IHubContext<PaymentHub> _hub;
        private readonly IUnitOfWork _unit;

        public PaymentTransactionHandlerRealtime(
            PaymentTransactionHandler inner,
            IHubContext<PaymentHub> hub,
            IUnitOfWork unit)
        {
            _inner = inner;
            _hub = hub;
            _unit = unit;
        }

        public Task<Response> CreateTransaction(CreatePaymentTransactionModel model, CancellationToken ct)
            => _inner.CreateTransaction(model, ct);

        public async Task<Response> WebhookTransaction(PaymentGatewayWebhookModel payload, CancellationToken ct)
        {
            if (payload == null)
                return await _inner.WebhookTransaction(payload!, ct);

            if (string.IsNullOrWhiteSpace(payload.TransactionKey))
                return await _inner.WebhookTransaction(payload, ct);

            var transactionId = payload.MerchantTransactionId;
            if (string.IsNullOrWhiteSpace(transactionId))
                return await _inner.WebhookTransaction(payload, ct);

            var oldEntity = await GetPaymentHistory(transactionId, ct);
            var oldStatus = oldEntity?.Status;

            var resp = await _inner.WebhookTransaction(payload, ct);
            if (resp.Code != Code.Success)
                return resp;

            var entity = await GetPaymentHistory(transactionId, ct);
            if (entity == null)
                return resp;

            if (oldStatus == entity.Status)
                return resp;

            await PushTransactionUpdated(entity, ct);
            return resp;
        }

        public Task<Response> GetByTransactionId(string transactionId, CancellationToken ct)
            => _inner.GetByTransactionId(transactionId, ct);

        public Task<Response> GetFastCreateHistories(string branchCode, CancellationToken ct)
            => _inner.GetFastCreateHistories(branchCode, ct);

        public async Task<Response> ManualApprove(string transactionId, string approvedByUserCode, string approvedByUserName, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
                return new Response(Code.BadRequest, "TransactionId is required");

            var oldEntity = await GetPaymentHistory(transactionId, ct);
            var oldStatus = oldEntity?.Status;

            var resp = await _inner.ManualApprove(transactionId, approvedByUserCode, approvedByUserName, ct);
            if (resp.Code != Code.Success)
                return resp;

            var entity = await GetPaymentHistory(transactionId, ct);
            if (entity == null)
                return resp;

            if (oldStatus == entity.Status)
                return resp;

            await PushTransactionUpdated(entity, ct);
            return resp;
        }

        private static string GetGroupName(string transactionId) => $"transaction:{transactionId}";

        private async Task<PaymentHistories?> GetPaymentHistory(string transactionId, CancellationToken ct)
        {
            var repo = _unit.GetRepository<PaymentHistories>();
            return await repo.Get(x => x.TransactionId == transactionId)
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);
        }

        private async Task PushTransactionUpdated(PaymentHistories entity, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(entity.TransactionId))
                return;

            try
            {
                var groupName = GetGroupName(entity.TransactionId);

                await _hub.Clients.Group(groupName)
                    .SendAsync(
                        "TransactionUpdated",
                        new
                        {
                            transactionId = entity.TransactionId,
                            status = entity.Status,
                            amountDue = entity.AmountDue,
                            amountPaid = entity.AmountPaid
                        },
                        ct);

                await SaveAndBroadcastNotification(entity, ct);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Push TransactionUpdated error. transactionId={TransactionId}", entity.TransactionId);
            }
        }

        private async Task SaveAndBroadcastNotification(PaymentHistories entity, CancellationToken ct)
        {
            if (entity.Status != 2) return;

            try
            {
                var salesOrderRepo = _unit.GetRepository<SalesOrder>();
                var salesOrderTableRepo = _unit.GetRepository<SalesOrderTable>();

                SalesOrder? order = null;
                var orderCode = entity.TransactionId ?? entity.DocumentNumber;
                if (!string.IsNullOrEmpty(orderCode))
                {
                    if (orderCode.StartsWith("PAY-"))
                    {
                        var parts = orderCode.Split('-');
                        if (parts.Length >= 2 && Guid.TryParse(parts[1], out var parsedId))
                        {
                            order = await salesOrderRepo.Get(x => x.Id == parsedId)
                                .AsNoTracking()
                                .FirstOrDefaultAsync(ct);
                        }
                    }
                    
                    if (order == null)
                    {
                        order = await salesOrderRepo.Get(x => x.OrderCode == orderCode)
                            .AsNoTracking()
                            .FirstOrDefaultAsync(ct);
                    }
                }

                string tableName = "";
                if (order != null)
                {
                    var orderTables = await salesOrderTableRepo.Get(x => x.OrderId == order.Id)
                        .AsNoTracking()
                        .ToListAsync(ct);
                        
                    if (orderTables != null && orderTables.Any())
                    {
                        tableName = string.Join(" + ", orderTables.OrderBy(ot => ot.TableName).Select(ot => ot.TableName));
                    }
                }

                var documentCode = order?.OrderCode ?? entity.DocumentNumber ?? entity.TransactionId;
                var displayCode = documentCode;
                if (!string.IsNullOrEmpty(tableName))
                {
                    displayCode = $"{documentCode} + {tableName}";
                }

                if (displayCode != null && displayCode.Length > 50)
                {
                    displayCode = displayCode.Substring(0, 47) + "...";
                }

                var message = $"Thanh toán thành công {(entity.AmountPaid ?? 0).ToString("N0")}đ cho hóa đơn {displayCode}";
                var notification = new PaymentNotification
                {
                    Id = Guid.NewGuid(),
                    BranchCode = entity.BranchCode ?? "",
                    TransactionId = entity.TransactionId ?? "",
                    DocumentNumber = displayCode,
                    Message = message,
                    Amount = entity.AmountPaid ?? 0,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                var repo = _unit.GetRepository<PaymentNotification>();
                repo.Add(notification);
                await _unit.SaveAsync();

                if (!string.IsNullOrWhiteSpace(entity.BranchCode))
                {
                    await _hub.Clients.Group($"branch:{entity.BranchCode}")
                        .SendAsync(
                            "ReceivePaymentNotification",
                            new
                            {
                                id = notification.Id,
                                branchCode = notification.BranchCode,
                                transactionId = notification.TransactionId,
                                documentNumber = notification.DocumentNumber,
                                message = notification.Message,
                                amount = notification.Amount,
                                isRead = notification.IsRead,
                                createdAt = notification.CreatedAt
                            },
                            ct);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SaveAndBroadcastNotification error. transactionId={TransactionId}", entity.TransactionId);
            }
        }
    }
}
