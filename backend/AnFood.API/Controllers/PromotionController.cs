using System;
using System.Threading.Tasks;
using AnFood.Application.Bussiness.PromotionBussiness;
using AnFood.Application.Helper;
using AnFood.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnFood.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PromotionController : ControllerBase
    {
        private readonly IPromotionHandler _promotionHandler;

        public PromotionController(IPromotionHandler promotionHandler)
        {
            _promotionHandler = promotionHandler;
        }

        [HttpPost]
        public async Task<Response> Create([FromBody] PromotionCreateModel model)
        {
            var userInfo = User.GetUserClaimsInfo();
            model.CreatedByUserId = userInfo.UserCode;
            model.CreatedByUserName = userInfo.Name;
            return await _promotionHandler.Create(model);
        }

        [HttpPut]
        public async Task<Response> Update([FromBody] PromotionUpdateModel model)
        {
            var userInfo = User.GetUserClaimsInfo();
            model.UpdatedByUserId = userInfo.UserCode;
            model.UpdatedByUserName = userInfo.Name;
            return await _promotionHandler.Update(model);
        }

        [HttpDelete]
        public async Task<Response> Delete([FromQuery] Guid id)
        {
            var userInfo = User.GetUserClaimsInfo();
            return await _promotionHandler.Delete(id, userInfo.UserCode, userInfo.Name);
        }

        [HttpGet("getby-id")]
        public async Task<Response> GetById([FromQuery] Guid id)
        {
            return await _promotionHandler.GetById(id);
        }

        [HttpPost("getby-filter")]
        public async Task<Response> GetByFilter([FromBody] PromotionFilterModel model)
        {
            return await _promotionHandler.GetByFilter(model);
        }
    }
}
