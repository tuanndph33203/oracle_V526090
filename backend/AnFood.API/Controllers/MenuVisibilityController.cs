using System;
using System.Threading.Tasks;
using AnFood.Application.Bussiness.MenuVisibility;
using AnFood.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnFood.API.Controllers;

[Authorize]
[ApiController]
[Route("api/menu-visibility")]
public class MenuVisibilityController(MenuVisibilityService service) : ControllerBase
{
    [HttpGet]
    public async Task<Response> GetAll() => await service.GetAll();

    [HttpGet("items")]
    public async Task<Response> GetItems() => await service.GetItems();
    [HttpGet("state")]
    public async Task<Response> GetState([FromQuery] string branchCode) =>
        new ResponseObject<string>(await service.GetStateAsync(branchCode), "Success", Code.Success);
    [HttpPost]
    public async Task<Response> Create(MenuVisibilityModel model) => await service.Save(model, true);

    [HttpPut]
    public async Task<Response> Update(MenuVisibilityModel model) => await service.Save(model, false);

    [HttpPatch("{id:guid}/enabled")]
    public async Task<Response> SetEnabled(Guid id, [FromBody] bool enabled) => await service.SetEnabled(id, enabled);

    [HttpDelete("{id:guid}")]
    public async Task<Response> Delete(Guid id) => await service.Delete(id);
}
