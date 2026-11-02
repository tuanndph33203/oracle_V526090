using AnFood.Application.Bussiness.WardBussiness;
using AnFood.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnFood.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WardController : ControllerBase
    {
        private readonly IWardHandler _wardHandler;
        public WardController(IWardHandler wardHandler)
        {
            _wardHandler = wardHandler;
        }
        [HttpPost]
        public async Task<Response> Create([FromBody] CreateWardModel model)
        {
            return await _wardHandler.Create(model);
        }
        [HttpPut]
        public async Task<Response> Update([FromBody] UpdateWardModel model)
        {
            return await _wardHandler.Update(model);
        }
        [HttpDelete]
        public async Task<Response> Delete(Guid Id)
        {
            return await _wardHandler.Delete(Id);
        }
        [HttpGet]
        [Route("getby-id")]
        public async Task<Response> GetById(Guid Id)
        {
            return await _wardHandler.GetById(Id);
        }
        [HttpPost]
        [Route("getby-filter")]
        public async Task<Response> GetByFilter([FromBody] WardFilterModel model)
        {
            return await _wardHandler.GetByFilter(model);
        }
    }
}
