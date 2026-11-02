using AnFood.Domain.Datatables;
using AnFood.Infrastructure;
using AnFood.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AnFood.API.Controllers.Payment
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentNotificationsController : ControllerBase
    {
        private readonly IUnitOfWork _unit;

        public PaymentNotificationsController(IUnitOfWork unit)
        {
            _unit = unit;
        }

        [HttpGet]
        public async Task<Response> GetNotifications(
            [FromQuery] string[] branchCodes, 
            [FromQuery] int limit = 50, 
            CancellationToken ct = default)
        {
            if (branchCodes == null || branchCodes.Length == 0)
            {
                return new Response(Code.BadRequest, "BranchCode is required");
            }

            var repo = _unit.GetRepository<PaymentNotification>();
            var notifications = await repo.Get(x => branchCodes.Contains(x.BranchCode))
                .OrderByDescending(x => x.CreatedAt)
                .Take(limit)
                .ToListAsync(ct);

            return new ResponseObject<List<PaymentNotification>>(notifications, "Success", Code.Success);
        }

        [HttpGet("unread-count")]
        public async Task<Response> GetUnreadCount(
            [FromQuery] string[] branchCodes, 
            CancellationToken ct = default)
        {
            if (branchCodes == null || branchCodes.Length == 0)
            {
                return new Response(Code.BadRequest, "BranchCode is required");
            }

            var repo = _unit.GetRepository<PaymentNotification>();
            var count = await repo.Get(x => branchCodes.Contains(x.BranchCode) && !x.IsRead)
                .CountAsync(ct);

            return new ResponseObject<int>(count, "Success", Code.Success);
        }

        [HttpPut("{id}/read")]
        public async Task<Response> MarkAsRead(Guid id, CancellationToken ct)
        {
            var repo = _unit.GetRepository<PaymentNotification>();
            var notification = await repo.Get(x => x.Id == id).FirstOrDefaultAsync(ct);
            if (notification == null)
            {
                return new Response(Code.NotFound, "Notification not found");
            }

            notification.IsRead = true;
            repo.Update(notification);
            await _unit.SaveAsync();

            return new Response(Code.Success, "Notification marked as read");
        }

        [HttpPut("mark-all-read")]
        public async Task<Response> MarkAllAsRead([FromBody] string[] branchCodes, CancellationToken ct)
        {
            if (branchCodes == null || branchCodes.Length == 0)
            {
                return new Response(Code.BadRequest, "BranchCode is required");
            }

            var repo = _unit.GetRepository<PaymentNotification>();
            var unreadNotifications = await repo.Get(x => branchCodes.Contains(x.BranchCode) && !x.IsRead)
                .ToListAsync(ct);

            foreach (var notif in unreadNotifications)
            {
                notif.IsRead = true;
                repo.Update(notif);
            }

            await _unit.SaveAsync();

            return new Response(Code.Success, "All notifications marked as read");
        }
    }
}
