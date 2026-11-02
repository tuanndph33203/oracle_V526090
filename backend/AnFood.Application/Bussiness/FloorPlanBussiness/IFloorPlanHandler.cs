using System;
using System.Threading.Tasks;
using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.FloorPlanBussiness
{
    public interface IFloorPlanHandler
    {
        Task<Response> Create(FloorPlanCreateModel model);
        Task<Response> Update(FloorPlanUpdateModel model);
        Task<Response> Delete(Guid id, string? userCode, string? userName);
        Task<Response> GetById(Guid id);
        Task<Response> GetByFilter(FloorPlanFilterModel model);
    }
}