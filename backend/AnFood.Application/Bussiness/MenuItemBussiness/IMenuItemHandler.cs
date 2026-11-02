using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.MenuItemBussiness
{
    public interface IMenuItemHandler
    {
        Task<Response> Create(MenuItemCreateModel model);
        Task<Response> Update(MenuItemUpdateModel model);
        Task<Response> Delete(Guid id, string? userCode, string? userName);
        Task<Response> GetById(Guid id);
        Task<Response> GetByFilter(MenuItemFilterModel model);
        Task<Response> ToggleAvailability(ToggleMenuItemAvailabilityModel model);
        Task<Response> ResetAllAvailability();
        Task<byte[]> ExportExcelAsync(MenuItemFilterModel model, System.Threading.CancellationToken ct);
        Task<byte[]> ExportExcelByIdsAsync(System.Collections.Generic.List<Guid> ids, System.Threading.CancellationToken ct);
    }
}