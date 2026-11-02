using System;
using System.Threading.Tasks;
using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.WarehouseBussiness
{
    public interface IWarehouseHandler
    {
        Task<Response> Create(WarehouseCreateModel model);
        Task<Response> Update(WarehouseUpdateModel model);
        Task<Response> Delete(Guid id, string? userCode, string? userName);
        Task<Response> GetById(Guid id);
        Task<Response> GetByFilter(WarehouseFilterModel model);
    }
}