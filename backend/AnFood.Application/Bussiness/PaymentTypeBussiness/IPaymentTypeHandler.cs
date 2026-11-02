using System;
using System.Threading.Tasks;
using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.PaymentTypeBussiness
{
    public interface IPaymentTypeHandler
    {
        Task<Response> Create(PaymentTypeCreateModel model);
        Task<Response> Update(PaymentTypeUpdateModel model);
        Task<Response> Delete(Guid id, string? userCode, string? userName);
        Task<Response> GetById(Guid id);
        Task<Response> GetByFilter(PaymentTypeFilterModel model);
    }
}