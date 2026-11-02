using System;
using System.Threading;
using System.Threading.Tasks;
using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.BusinessPartnerBussiness
{
    public interface IBusinessPartnerHandler
    {
        Task<Response> Create(BusinessPartnerCreateModel model);
        Task<Response> Update(BusinessPartnerUpdateModel model);
        Task<Response> Delete(Guid id, string? userCode, string? userName);
        Task<Response> GetById(Guid id);
        Task<Response> GetByFilter(BusinessPartnerFilterModel model);
        Task<Response> ImportHoAsync(System.IO.Stream fileStream, string? createdByUserCode, string? createdByUserName, bool isCsv, CancellationToken ct);
        Task<Response> GetNextCode(int partnerType);
    }
}
