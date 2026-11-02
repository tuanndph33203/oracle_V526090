using System;
using System.Threading.Tasks;
using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.MenuGroupBussiness
{
    public interface IMenuGroupHandler
    {
        Task<Response> Create(MenuGroupCreateModel model);
        Task<Response> Update(MenuGroupUpdateModel model);
        Task<Response> Delete(Guid id, string? userCode, string? userName);
        Task<Response> GetById(Guid id);
        Task<Response> GetByFilter(MenuGroupFilterModel model);
    }
}