using AnFood.Application.Bussiness.DistrictBussiness;
using AnFood.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnFood.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DistrictController : ControllerBase
    {
        private readonly IDistrictHandler _districtHandler;
        public DistrictController(IDistrictHandler districtHandler)
        {
            _districtHandler = districtHandler;
        }
        [HttpPost]
        public async Task<Response> Create([FromBody] DistrictCreateModel model)
        {
            return await _districtHandler.Create(model);
        }
        [HttpPut]
        public async Task<Response> Update([FromBody] DistrictUpdateModel model)
        {
            return await _districtHandler.Update(model);
        }
        [HttpDelete]
        public async Task<Response> Delete(Guid Id)
        {
            return await _districtHandler.Delete(Id);
        }
        [HttpGet]
        [Route("getby-id")]
        public async Task<Response> GetById(Guid Id)
        {
            return await _districtHandler.GetById(Id);
        }
        [HttpPost]
        [Route("getby-filter")]
        public async Task<Response> GetByFilter([FromBody] DistrictFilterModel model)
        {
            return await _districtHandler.GetByFilter(model);
        }
    }
}
