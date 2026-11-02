using AnFood.Domain.Datatables;
using AnFood.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AnFood.API.Hubs
{
    [Authorize]
    public class PaymentHub : Hub
    {
        private readonly IUnitOfWork _unit;

        public PaymentHub(IUnitOfWork unit)
        {
            _unit = unit;
        }

        private static string GetGroupName(string transactionId) => $"transaction:{transactionId}";

        public async Task JoinBranches(string[] branchCodes)
        {
            try
            {
                if (branchCodes == null || branchCodes.Length == 0) return;
                foreach (var branchCode in branchCodes)
                {
                    if (!string.IsNullOrWhiteSpace(branchCode))
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, $"branch:{branchCode}");
                        Log.Information($"PaymentHub: Connection {Context.ConnectionId} joined branch:{branchCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "PaymentHub.JoinBranches error");
                throw;
            }
        }

        public async Task LeaveBranch(string branchCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(branchCode)) return;
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"branch:{branchCode}");
                Log.Information($"PaymentHub: Connection {Context.ConnectionId} left branch:{branchCode}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "PaymentHub.LeaveBranch error");
            }
        }

        public async Task JoinTransaction(string transactionId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(transactionId))
                {
                    Log.Warning("TransactionId is null/empty");
                    return;
                }

                var repo = _unit.GetRepository<PaymentHistories>();

                var entity = await repo.Get(x => x.TransactionId == transactionId)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                var groupName = GetGroupName(transactionId);

                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

                if (entity != null)
                {
                    await Clients.Caller.SendAsync(
                        "TransactionUpdated",
                        new
                        {
                            transactionId = entity.TransactionId,
                            status = entity.Status,
                            amountDue = entity.AmountDue,
                            amountPaid = entity.AmountPaid
                        },
                        CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "PaymentHub.JoinTransaction error");
            }
        }
    }
}
