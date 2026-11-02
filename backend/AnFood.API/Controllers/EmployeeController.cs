using AnFood.Application.Bussiness.EmployeeBussiness;
using AnFood.Application.Helper;
using AnFood.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnFood.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeHandler _handler;

        public EmployeeController(IEmployeeHandler handler)
        {
            _handler = handler;
        }

        [HttpPost]
        public async Task<Response> Create([FromBody] CreateEmployeeModel model)
        {
            var getInfoAccount = User.GetUserClaimsInfo();
            model.CreatedByUserId = getInfoAccount.UserCode;
            model.CreatedByUserName = getInfoAccount.Name;
            return await _handler.Create(model);
        }

        [HttpPut]
        public async Task<Response> Update([FromBody] UpdateEmployeeModel model)
        {
            var getInfoAccount = User.GetUserClaimsInfo();
            model.UpdatedByUserId = getInfoAccount.UserCode;
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

        [HttpGet]
        [Route("getby-code")]
        public async Task<Response> GetByCode(string code, CancellationToken ct)
        {
            return await _handler.GetByCode(code, ct);
        }

        [HttpPost]
        [Route("getby-filter")]
        public async Task<Response> GetByFilter([FromBody] EmployeeFilterModel model)
        {
            return await _handler.GetByFilter(model);
        }

        [HttpGet]
        [Route("create-code")]
        public async Task<Response> CreateCode(string branchCode, CancellationToken ct)
        {
            return await _handler.CreateCode(branchCode, ct);
        }
    }
}
