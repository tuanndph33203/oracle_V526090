using System;
using System.Threading;
using System.Threading.Tasks;
using AnFood.Application.Bussiness.BusinessPartnerBussiness;
using AnFood.Application.Helper;
using AnFood.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnFood.API.Controllers
{
    public class BusinessPartnerHoImportForm
    {
        public Microsoft.AspNetCore.Http.IFormFile? File { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BusinessPartnerController : ControllerBase
    {
        private readonly IBusinessPartnerHandler _handler;

        public BusinessPartnerController(IBusinessPartnerHandler handler)
        {
            _handler = handler;
        }

        [HttpPost]
        public async Task<Response> Create([FromBody] BusinessPartnerCreateModel model)
        {
            var getInfoAccount = User.GetUserClaimsInfo();
            model.CreatedByUserCode = getInfoAccount.UserCode;
            model.CreatedByUserName = getInfoAccount.Name;
            return await _handler.Create(model);
        }

        [HttpPut]
        public async Task<Response> Update([FromBody] BusinessPartnerUpdateModel model)
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
        public async Task<Response> GetByFilter([FromBody] BusinessPartnerFilterModel model)
        {
            return await _handler.GetByFilter(model);
        }

        [HttpGet]
        [Route("get-next-code")]
        public async Task<Response> GetNextCode([FromQuery] int partnerType = 1)
        {
            return await _handler.GetNextCode(partnerType);
        }

        [HttpPost("import-ho")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(20_000_000)]
        public async Task<Response> ImportHo([FromForm] BusinessPartnerHoImportForm form, CancellationToken ct)
        {
            var file = form.File;
            if (file == null || file.Length == 0) return new Response(Code.BadRequest, "Vui lòng chọn file Excel.");
            var extension = System.IO.Path.GetExtension(file.FileName);
            if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase) && !string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
                return new Response(Code.BadRequest, "Chỉ hỗ trợ file .xlsx hoặc .csv.");
            var user = User.GetUserClaimsInfo();
            await using var stream = file.OpenReadStream();
            return await _handler.ImportHoAsync(stream, user.UserCode, user.Name, string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase), ct);
        }
    }
}
