using System;
using System.Threading.Tasks;
using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.PreparationAreaBussiness
{
    public interface IPreparationAreaHandler
    {
        Task<Response> Create(PreparationAreaCreateModel model);
        Task<Response> Update(PreparationAreaUpdateModel model);
        Task<Response> Delete(Guid id, string? userCode, string? userName);
        Task<Response> GetById(Guid id);
        Task<Response> GetByFilter(PreparationAreaFilterModel model);
    }
}