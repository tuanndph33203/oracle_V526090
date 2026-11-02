using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnFood.Application.Bussiness.ShiftBussiness;
using AnFood.Application.Helper;
using AnFood.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnFood.API.Controllers
{
    [Route("api/shifts")]
    [ApiController]
    [Authorize]
    public class ShiftController : ControllerBase
    {
        private readonly IShiftHandler _handler;

        public ShiftController(IShiftHandler handler)
        {
            _handler = handler;
        }

        [HttpPost("open")]
        public async Task<Response> OpenShift([FromBody] ShiftOpenModel model)
        {
            var user = User.GetUserClaimsInfo();
            return await _handler.OpenShift(model, user.UserCode ?? "system", user.Name ?? "system");
        }

        [HttpGet("current")]
        public async Task<Response> GetCurrentShift([FromQuery] string branchCode, [FromQuery] string? machineCode)
        {
            var user = User.GetUserClaimsInfo();
            return await _handler.GetCurrentShift(branchCode, machineCode, user.UserCode ?? "system");
        }

        [HttpPost("extend")]
        public async Task<Response> ExtendShift([FromBody] ShiftExtendModel model)
        {
            var user = User.GetUserClaimsInfo();
            return await _handler.ExtendShift(model, user.UserCode ?? "system", user.Name ?? "system");
        }

        [HttpGet("{id}/summary")]
        public async Task<Response> GetShiftSummary([FromRoute] Guid id)
        {
            return await _handler.GetShiftSummary(id);
        }

        [HttpPost("{id}/cash-count")]
        public async Task<Response> SubmitCashCount([FromRoute] Guid id, [FromBody] List<ShiftCashCountItemModel> items)
        {
            var user = User.GetUserClaimsInfo();
            return await _handler.SubmitCashCount(id, items, user.UserCode ?? "system", user.Name ?? "system");
        }

        [HttpPost("close")]
        public async Task<Response> CloseShift([FromBody] ShiftCloseModel model)
        {
            var user = User.GetUserClaimsInfo();
            return await _handler.CloseShift(model, user.UserCode ?? "system", user.Name ?? "system");
        }

        [HttpPost("history")]
        public async Task<Response> GetShiftHistory([FromBody] ShiftFilterModel model)
        {
            return await _handler.GetShiftHistory(model);
        }

        [HttpGet("{id}")]
        public async Task<Response> GetShiftById([FromRoute] Guid id)
        {
            return await _handler.GetShiftById(id);
        }
    }
}
