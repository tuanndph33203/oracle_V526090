using System;
using System.Threading.Tasks;
using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.TaxRateBussiness
{
    public interface ITaxRateHandler
    {
        Task<Response> Create(TaxRateCreateModel model);
        Task<Response> Update(TaxRateUpdateModel model);
        Task<Response> Delete(Guid id, string? userCode, string? userName);
        Task<Response> GetById(Guid id);
        Task<Response> GetByFilter(TaxRateFilterModel model);
    }
}