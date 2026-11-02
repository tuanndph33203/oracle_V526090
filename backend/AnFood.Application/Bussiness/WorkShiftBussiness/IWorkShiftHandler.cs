using System;
using System.Threading.Tasks;
using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.WorkShiftBussiness
{
    public interface IWorkShiftHandler
    {
        Task<Response> Create(WorkShiftCreateModel model);
        Task<Response> Update(WorkShiftUpdateModel model);
        Task<Response> Delete(Guid id, string? userCode, string? userName);
        Task<Response> GetById(Guid id);
        Task<Response> GetByFilter(WorkShiftFilterModel model);
    }
}