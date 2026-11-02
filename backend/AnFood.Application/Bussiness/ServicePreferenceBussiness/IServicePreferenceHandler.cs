using System;
using System.Threading.Tasks;
using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.ServicePreferenceBussiness
{
    public interface IServicePreferenceHandler
    {
        Task<Response> Create(ServicePreferenceCreateModel model);
        Task<Response> Update(ServicePreferenceUpdateModel model);
        Task<Response> Delete(Guid id, string? userCode, string? userName);
        Task<Response> GetById(Guid id);
        Task<Response> GetByFilter(ServicePreferenceFilterModel model);
    }
}