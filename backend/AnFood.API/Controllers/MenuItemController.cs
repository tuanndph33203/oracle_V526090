using System;
using System.Threading.Tasks;
using AnFood.Application.Bussiness.MenuItemBussiness;
using AnFood.Application.Helper;
using AnFood.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnFood.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MenuItemController : ControllerBase
    {
        private readonly IMenuItemHandler _handler;

        public MenuItemController(IMenuItemHandler handler)
        {
            _handler = handler;
        }

        [HttpPost]
        public async Task<Response> Create([FromBody] MenuItemCreateModel model)
        {
            var getInfoAccount = User.GetUserClaimsInfo();
            model.CreatedByUserCode = getInfoAccount.UserCode;
            model.CreatedByUserName = getInfoAccount.Name;
            return await _handler.Create(model);
        }

        
        [HttpPut]
        public async Task<Response> Update([FromBody] MenuItemUpdateModel model)
        {
            var getInfoAccount = User.GetUserClaimsInfo();
            model.UpdatedByUserCode = getInfoAccount.UserCode;
            model.UpdatedByUserName = getInfoAccount.Name;
            return await _handler.Update(model);
        }

        [HttpDelete]
        public async Task<Response> Delete(Guid id)
        {
            var getInfoAccount = User.GetUserClaimsInfo();
            return await _handler.Delete(id, getInfoAccount.UserCode, getInfoAccount.Name);
        }

        [HttpGet]
        [Route("getby-id")]
        public async Task<Response> GetById(Guid id)
        {
            return await _handler.GetById(id);
        }

        [HttpPost]
        [Route("getby-filter")]
        public async Task<Response> GetByFilter([FromBody] MenuItemFilterModel model)
        {
            return await _handler.GetByFilter(model);
        }

        [HttpPatch]
        [Route("toggle-availability")]
        public async Task<Response> ToggleAvailability([FromBody] ToggleMenuItemAvailabilityModel model)
        {
            var getInfoAccount = User.GetUserClaimsInfo();
            model.UpdatedByUserCode = getInfoAccount.UserCode;
            model.UpdatedByUserName = getInfoAccount.Name;
            return await _handler.ToggleAvailability(model);
        }

        [HttpPost]
        [Route("reset-availability")]
        public async Task<Response> ResetAllAvailability()
        {
            return await _handler.ResetAllAvailability();
        }

        [HttpPost]
        [Route("export-excel")]
        public async Task<IActionResult> ExportExcel([FromBody] MenuItemFilterModel? model, System.Threading.CancellationToken ct)
        {
            var bytes = await _handler.ExportExcelAsync(model ?? new MenuItemFilterModel(), ct);
            var fileName = $"menu_items_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
