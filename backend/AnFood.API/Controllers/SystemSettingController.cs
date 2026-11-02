using AnFood.Application.Bussiness.SystemSettingBusiness;
using AnFood.Domain.Datatables;
using AnFood.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AnFood.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SystemSettingController : ControllerBase
    {
        private readonly ISystemSettingHandler _service;
        public SystemSettingController(ISystemSettingHandler handler)
        {
            _service = handler;
        }
        [HttpPost]
        public async Task<Response> Create([FromBody] CreateSystemSettingCommand model, CancellationToken ct)
        {
            var result = await _service.Create(model, ct);
            return result;
        }

        [HttpPut]
        public async Task<Response> Update([FromBody] UpdateSystemSettingCommand model, CancellationToken ct)
        {
            var result = await _service.Update(model, ct);
            return result;
        }

        [HttpPut("bulk")]
        public async Task<Response> UpdateMultiple([FromBody] UpdateMultipleSystemSettingCommand model, CancellationToken ct)
        {
            var result = await _service.UpdateMultiple(model, ct);
            return result;
        }

        [HttpDelete("{id}")]
        public async Task<Response> Delete(Guid id, CancellationToken ct)
        {
            var result = await _service.Delete(id, ct);
            return result;
        }

        [HttpGet("getall")]
        public async Task<Response> GetByFilter(CancellationToken ct)
        {
            var result = await _service.GetByFilter(ct);
            return result;
        }
    }
}
