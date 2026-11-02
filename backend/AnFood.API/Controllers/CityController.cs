using AnFood.Application.Bussiness.CityBussiness;
using AnFood.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnFood.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CityController : ControllerBase
    {
        private readonly ICityHandler _cityHandler;
        public CityController(ICityHandler cityHandler)
        {
            _cityHandler = cityHandler;
        }
        [HttpPost]
        public async Task<Response> Create([FromBody] CityCreateModel model )
        {
            return await _cityHandler.Create(model);
        }
        [HttpPut]
        public async Task<Response> Update([FromBody] CityUpdateModel model)
        {
            return await _cityHandler.Update(model);
        }
        [HttpDelete]
        public async Task<Response> Delete(Guid Id)
        {
            return await _cityHandler.Delete(Id);
        }
        [HttpGet]
        [Route("getby-id")]
        public async Task<Response> GetById (Guid Id)
        {
            return await _cityHandler.GetById(Id);
        }
        [HttpPost]
        [Route("getby-filter")]
        public async Task<Response> GetByFilter([FromBody] CityFilterModel model)
        {
            return await _cityHandler.GetByFilter(model);
        }
    }
}
