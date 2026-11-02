using AnFood.Application.Bussiness.SystemBussiness.AccountBussiness;
using AnFood.Application.Contant;
using AnFood.Application.Helper;
using AnFood.Application.Services.Mqtt;
using AnFood.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AnFood.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
   // [Authorize]
    public class SystemController : ControllerBase
    {
        private readonly IAccountingHandler _accountHandler;
        private readonly IMqttSpeakerService _mqttSpeakerService;

        public SystemController(IAccountingHandler accountHandler, IMqttSpeakerService mqttSpeakerService)
        {
            _accountHandler = accountHandler;
            _mqttSpeakerService = mqttSpeakerService;
        }
        [HttpPost("account")]
        public async Task<Response> CreateAccount(CreateAccountModel model, CancellationToken ct)
        {
            var getInfoAccount = User.GetUserClaimsInfo();
            model.CreatedByUserCode = getInfoAccount.UserCode;
            model.CreatedByUserName = getInfoAccount.Name;
            var response = await _accountHandler.Create(model, ct);
            return response;
        }
        [HttpPost("account/getby-filter")]
        public async Task<Response> GetAll([FromBody] FilterModel model, CancellationToken ct)
        {
            var response = await _accountHandler.GetAll(model, ct);
            return response;
        }
        [HttpPut("account")]
        public async Task<Response> Update(UpdateAccountModel model, CancellationToken ct)
        {
            var response = await _accountHandler.Update(model, ct);
            return response;
        }
        [HttpDelete("account")]
        public async Task<Response> Delete(string Id, CancellationToken ct)
        {
            var response = await _accountHandler.Delete(Id, ct);
            return response;
        }

        [HttpGet("account/getby-id")]
        public async Task<Response> GetById(string Id, CancellationToken ct)
        {
         
             return await _accountHandler.GetById(Id, ct);
        }

        [HttpPut("account/multiple")]
        public async Task<Response> UpdateMutiple([FromBody] List<UpdateAccountModel> models, CancellationToken ct)
        {
            var response = await _accountHandler.UpdateMutiple(models, ct);
            return response;
        }

        [HttpPost("test-mqtt-speaker")]
        public async Task<Response> TestMqttSpeaker([FromQuery] string? message = "1")
        {
            var msg = string.IsNullOrWhiteSpace(message) ? "1" : message;
            await _mqttSpeakerService.PublishSpeakerAsync(msg);
            return new Response(Code.Success, $"Đã gửi chuỗi '{msg}' vào topic test_loa/state thành công!");
        }
    }
}
