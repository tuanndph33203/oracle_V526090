using System;
using System.Threading.Tasks;
using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.CashDrawerBussiness
{
    public interface ICashDrawerHandler
    {
        Task<Response> Create(CashDrawerCreateModel model);
        Task<Response> Update(CashDrawerUpdateModel model);
        Task<Response> Delete(Guid id, string? userCode, string? userName);
        Task<Response> GetById(Guid id);
        Task<Response> GetByFilter(CashDrawerFilterModel model);
    }
}