using AnFood.Infrastructure;
using System;
using System.Threading.Tasks;

namespace AnFood.Application.Bussiness.KitchenBussiness
{
    public interface IKitchenHandler
    {
        Task<Response> SendKitchen(Guid orderId, SendKitchenRequestDto request);
        Task<Response> GetRequests(string branchCode, string? kitchenAreaIds, Guid? menuItemId);
        Task<Response> GetSummary(string branchCode, string? kitchenAreaIds);
        Task<Response> ServeItems(ServeItemsRequestDto request);
        Task<Response> SoldOutItems(SoldOutItemsRequestDto request);
    }
}
