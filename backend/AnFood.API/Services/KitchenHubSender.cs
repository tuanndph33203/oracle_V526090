using AnFood.API.Hubs;
using AnFood.Application.Bussiness.KitchenBussiness;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace AnFood.API.Services
{
    public class KitchenHubSender : IKitchenHubSender
    {
        private readonly IHubContext<KitchenHub> _hubContext;

        public KitchenHubSender(IHubContext<KitchenHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendNewRequestAsync(string branchCode, object request)
        {
            await _hubContext.Clients.Group($"branch:{branchCode}").SendAsync("NewKitchenRequest", request);
        }

        public async Task SendRequestsUpdatedAsync(string branchCode)
        {
            await _hubContext.Clients.Group($"branch:{branchCode}").SendAsync("KitchenRequestsUpdated");
        }
    }
}
