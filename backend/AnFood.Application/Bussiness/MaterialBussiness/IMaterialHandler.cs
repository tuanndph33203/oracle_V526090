using System;
using System.Threading.Tasks;
using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.MaterialBussiness
{
    public interface IMaterialHandler
    {
        Task<Response> Create(MaterialCreateModel model);
        Task<Response> Update(MaterialUpdateModel model);
        Task<Response> Delete(Guid id, string? userCode, string? userName);
        Task<Response> GetById(Guid id);
        Task<Response> GetByFilter(MaterialFilterModel model);
    }
}