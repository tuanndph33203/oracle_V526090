using AnFood.Application.Bussiness.ProductGroupBussiness;
using AnFood.Application.Helper;
using AnFood.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AnFood.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductGroupController : ControllerBase
    {
        private readonly IProductGroupHandler _handler;
        public ProductGroupController(IProductGroupHandler handler)
        {
            _handler = handler;
        }
        [HttpPost]
        public async Task<Response> Create([FromBody] CreateProductGroupModel model)
        {
            var getInfoAccount = User.GetUserClaimsInfo();
            model.CreatedByUserId = getInfoAccount.UserCode ;
            model.CreatedByUserName = getInfoAccount.Name;
            return await _handler.Create(model);
        }
        [HttpPut]
        public async Task<Response> Update([FromBody] UpdateProductGroupModel model)
        {
            return await _handler.Update(model);
        }
        [HttpDelete]
        public async Task<Response> Delete(Guid Id)
        {
            return await _handler.Delete(Id);
        }
        [HttpGet]
        [Route("getby-id")]
        public async Task<Response> GetById(Guid Id)
        {
            return await _handler.GetById(Id);
        }
        [HttpPost]
        [Route("getby-filter")]
        public async Task<Response> GetByFilter([FromBody] ProductGroupFilterModel model)
        {
            return await _handler.GetByFilter(model);
        }
    }
}
