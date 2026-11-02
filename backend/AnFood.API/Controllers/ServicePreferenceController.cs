using System;
using System.Threading.Tasks;
using AnFood.Application.Bussiness.ServicePreferenceBussiness;
using AnFood.Application.Helper;
using AnFood.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnFood.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ServicePreferenceController : ControllerBase
    {
        private readonly IServicePreferenceHandler _handler;

        public ServicePreferenceController(IServicePreferenceHandler handler)
        {
            _handler = handler;
        }

        [HttpPost]
        public async Task<Response> Create([FromBody] ServicePreferenceCreateModel model)
        {
            var getInfoAccount = User.GetUserClaimsInfo();
            model.CreatedByUserCode = getInfoAccount.UserCode;
            model.CreatedByUserName = getInfoAccount.Name;
            return await _handler.Create(model);
        }

        [HttpPut]
        public async Task<Response> Update([FromBody] ServicePreferenceUpdateModel model)
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
        public async Task<Response> GetByFilter([FromBody] ServicePreferenceFilterModel model)
        {
            return await _handler.GetByFilter(model);
        }
    }
}
