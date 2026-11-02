using System;
using System.Threading.Tasks;
using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.BusinessPartnerGroupBussiness
{
    public interface IBusinessPartnerGroupHandler
    {
        Task<Response> Create(BusinessPartnerGroupCreateModel model);
        Task<Response> Update(BusinessPartnerGroupUpdateModel model);
        Task<Response> Delete(Guid id, string? userCode, string? userName);
        Task<Response> GetById(Guid id);
        Task<Response> GetByFilter(BusinessPartnerGroupFilterModel model);
    }
}