using System;
using System.Threading.Tasks;
using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.DiningTableBussiness
{
    public interface IDiningTableHandler
    {
        Task<Response> Create(DiningTableCreateModel model);
        Task<Response> Update(DiningTableUpdateModel model);
        Task<Response> Delete(Guid id, string? userCode, string? userName);
        Task<Response> GetById(Guid id);
        Task<Response> GetByFilter(DiningTableFilterModel model);
        Task<Response> SaveFloorLayout(FloorLayoutUpdateModel model, string? userCode, string? userName);

        // Stub methods for ordering and billing
        Task<Response> OpenTable(Guid tableId, string branchCode);
        Task<Response> CreateOrder(Guid tableId, string branchCode);
        Task<Response> AddOrderItem(Guid orderId, object item);
        Task<Response> UpdateOrderItem(Guid orderId, object item);
        Task<Response> RemoveOrderItem(Guid orderId, Guid itemId);
        Task<Response> UpdateBill(Guid tableId, string branchCode);
        Task<Response> GetCurrentBill(Guid tableId, string branchCode);
        Task<Response> CloseBill(Guid tableId, string branchCode);
    }
}