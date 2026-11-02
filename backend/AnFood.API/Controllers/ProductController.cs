using AnFood.Application.Bussiness.ProductBussiness;
using AnFood.Application.Helper;
using AnFood.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AnFood.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
 //   [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly IProductHandler _handler;
        public ProductController(IProductHandler handler)
        {
            _handler = handler;
        }
        [HttpPost]
        public async Task<Response> Create([FromBody] CreateProductModel model)
        {
            var getInfoAccount = User.GetUserClaimsInfo();
            model.CreatedByUserCode = getInfoAccount.UserCode;
            model.CreatedByUserName = getInfoAccount.Name;
            return await _handler.Create(model);
        }
        [HttpPut]
        public async Task<Response> Update([FromBody] UpdateProductModel model)
        {
            return await _handler.Update(model);
        }
        [HttpDelete]
        public async Task<Response> Delete(Guid Id)
        {
            return await _handler.Delete(Id);
        }
   
        [HttpPost]
        [Route("getby-filter")]
        public async Task<Response> GetByFilter([FromBody] ProductFilterModel model)
        {
            return await _handler.GetByFilter(model);
        }

        [HttpPost]
        [Route("export-excel")]
        public async Task<IActionResult> ExportExcel([FromBody] List<Guid> ids, CancellationToken ct)
        {
            var bytes = await _handler.ExportExcelByIdsAsync(ids, ct);
            var fileName = $"products_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
