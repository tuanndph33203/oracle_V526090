using System;
using System.Threading.Tasks;
using AnFood.Application.Bussiness.DiningTableBussiness;
using AnFood.Application.Helper;
using AnFood.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnFood.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DiningTableController : ControllerBase
    {
        private readonly IDiningTableHandler _handler;

        public DiningTableController(IDiningTableHandler handler)
        {
            _handler = handler;
        }

        [HttpPost]
        public async Task<Response> Create([FromBody] DiningTableCreateModel model)
        {
            var getInfoAccount = User.GetUserClaimsInfo();
            model.CreatedByUserCode = getInfoAccount.UserCode;
            model.CreatedByUserName = getInfoAccount.Name;
            return await _handler.Create(model);
        }

        [HttpPut]
        public async Task<Response> Update([FromBody] DiningTableUpdateModel model)
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
        public async Task<Response> GetByFilter([FromBody] DiningTableFilterModel model)
        {
            return await _handler.GetByFilter(model);
        }

        [HttpPost]
        [Route("save-layout")]
        public async Task<Response> SaveLayout([FromBody] FloorLayoutUpdateModel model)
        {
            var getInfoAccount = User.GetUserClaimsInfo();
            return await _handler.SaveFloorLayout(model, getInfoAccount.UserCode, getInfoAccount.Name);
        }

        // ==========================================
        // Billing & Ordering Stubs / Placeholders
        // ==========================================

        [HttpPost]
        [Route("open-table")]
        public async Task<Response> OpenTable(Guid tableId, string branchCode)
        {
            return await _handler.OpenTable(tableId, branchCode);
        }

        [HttpPost]
        [Route("create-order")]
        public async Task<Response> CreateOrder(Guid tableId, string branchCode)
        {
            return await _handler.CreateOrder(tableId, branchCode);
        }

        [HttpPost]
        [Route("add-item")]
        public async Task<Response> AddOrderItem(Guid orderId, [FromBody] object item)
        {
            return await _handler.AddOrderItem(orderId, item);
        }

        [HttpPut]
        [Route("update-item")]
        public async Task<Response> UpdateOrderItem(Guid orderId, [FromBody] object item)
        {
            return await _handler.UpdateOrderItem(orderId, item);
        }

        [HttpDelete]
        [Route("remove-item")]
        public async Task<Response> RemoveOrderItem(Guid orderId, Guid itemId)
        {
            return await _handler.RemoveOrderItem(orderId, itemId);
        }

        [HttpPut]
        [Route("update-bill")]
        public async Task<Response> UpdateBill(Guid tableId, string branchCode)
        {
            return await _handler.UpdateBill(tableId, branchCode);
        }

        [HttpGet]
        [Route("current-bill")]
        public async Task<Response> GetCurrentBill(Guid tableId, string branchCode)
        {
            return await _handler.GetCurrentBill(tableId, branchCode);
        }

        [HttpPost]
        [Route("close-bill")]
        public async Task<Response> CloseBill(Guid tableId, string branchCode)
        {
            return await _handler.CloseBill(tableId, branchCode);
        }
    }
}
