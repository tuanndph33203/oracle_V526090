using AnFood.Application.Bussiness.BranchBussiness;
using AnFood.Application.Helper;
using AnFood.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnFood.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BranchController : ControllerBase
    {
        private readonly IBranchHandler _branchHandler;

        public BranchController(IBranchHandler branchHandler)
        {
            _branchHandler = branchHandler;
        }

        [HttpPost]
        public async Task<Response> Create([FromBody] BranchCreateModel model)
        {
            var getInfoAccount = User.GetUserClaimsInfo();
            model.CreatedByUserId = getInfoAccount.UserCode;
            model.CreatedByUserName = getInfoAccount.Name;
            return await _branchHandler.Create(model);
        }

        [HttpPut]
        public async Task<Response> Update([FromBody] BranchUpdateModel model)
        {
            var getInfoAccount = User.GetUserClaimsInfo();
            model.UpdatedByUserId = getInfoAccount.UserCode;
            model.UpdatedByUserName = getInfoAccount.Name;
            return await _branchHandler.Update(model);
        }

        [HttpDelete]
        public async Task<Response> Delete(Guid id)
        {
            var getInfoAccount = User.GetUserClaimsInfo();
            return await _branchHandler.Delete(id, getInfoAccount.UserCode, getInfoAccount.Name);
        }

        [HttpGet]
        [Route("getby-id")]
        public async Task<Response> GetById(Guid id)
        {
            return await _branchHandler.GetById(id);
        }

        [HttpPost]
        [Route("getby-filter")]
        public async Task<Response> GetByFilter([FromBody] BranchFilterModel model)
        {
            return await _branchHandler.GetByFilter(model);
        }
    }
}
