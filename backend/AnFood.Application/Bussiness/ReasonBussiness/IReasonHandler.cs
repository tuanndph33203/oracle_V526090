using System;
using System.Threading.Tasks;
using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.ReasonBussiness
{
    public interface IReasonHandler
    {
        Task<Response> Create(ReasonCreateModel model);
        Task<Response> Update(ReasonUpdateModel model);
        Task<Response> Delete(Guid id, string? userCode, string? userName);
        Task<Response> GetById(Guid id);
        Task<Response> GetByFilter(ReasonFilterModel model);
    }
}