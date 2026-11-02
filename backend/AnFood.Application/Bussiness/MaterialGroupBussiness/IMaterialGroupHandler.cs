using System;
using System.Threading.Tasks;
using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.MaterialGroupBussiness
{
    public interface IMaterialGroupHandler
    {
        Task<Response> Create(MaterialGroupCreateModel model);
        Task<Response> Update(MaterialGroupUpdateModel model);
        Task<Response> Delete(Guid id, string? userCode, string? userName);
        Task<Response> GetById(Guid id);
        Task<Response> GetByFilter(MaterialGroupFilterModel model);
    }
}