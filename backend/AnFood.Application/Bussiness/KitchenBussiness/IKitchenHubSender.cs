using System.Threading.Tasks;

namespace AnFood.Application.Bussiness.KitchenBussiness
{
    public interface IKitchenHubSender
    {
        Task SendNewRequestAsync(string branchCode, object request);
        Task SendRequestsUpdatedAsync(string branchCode);
    }
}
