using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using System;
using System.Threading.Tasks;

namespace AnFood.API.Hubs
{
    [Authorize]
    public class KitchenHub : Hub
    {
        public async Task JoinBranch(string branchCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(branchCode)) return;
                await Groups.AddToGroupAsync(Context.ConnectionId, $"branch:{branchCode}");
                Log.Information($"KitchenHub: Connection {Context.ConnectionId} joined branch:{branchCode}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "KitchenHub.JoinBranch error");
            }
        }

        public async Task LeaveBranch(string branchCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(branchCode)) return;
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"branch:{branchCode}");
                Log.Information($"KitchenHub: Connection {Context.ConnectionId} left branch:{branchCode}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "KitchenHub.LeaveBranch error");
            }
        }
    }
}
