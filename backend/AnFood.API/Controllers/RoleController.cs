using AnFood.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnFood.API.Controllers
{
    [ApiController]
    [Route("api")]
    [AllowAnonymous]
    public class RoleController : ControllerBase
    {
        private static readonly List<object> DefaultRoles = new()
        {
            new { id = "admin-role", code = "ADMIN", name = "Quản trị viên", description = "Toàn quyền hệ thống", isActive = true },
            new { id = "cashier-role", code = "CASHIER", name = "Thu ngân", description = "Bán hàng và thu ngân", isActive = true },
            new { id = "barista-role", code = "BARISTA", name = "Pha chế", description = "Quản lý quầy bar & pha chế", isActive = true },
            new { id = "waiter-role", code = "WAITER", name = "Phục vụ", description = "Order và phục vụ bàn", isActive = true }
        };

        [HttpGet("role")]
        public IActionResult GetRoles([FromQuery] int? page = 1, [FromQuery] int? size = 20)
        {
            return Ok(new
            {
                code = 200,
                message = "Success",
                data = new
                {
                    content = DefaultRoles,
                    totalElements = DefaultRoles.Count,
                    totalPages = 1,
                    page = page ?? 1,
                    size = size ?? 20
                }
            });
        }

        [HttpGet("role/getby-id")]
        public IActionResult GetRoleById([FromQuery] string id)
        {
            var role = DefaultRoles.FirstOrDefault();
            return Ok(new
            {
                code = 200,
                message = "Success",
                data = role
            });
        }

        [HttpPost("role")]
        public IActionResult CreateRole([FromBody] object data)
        {
            return Ok(new { code = 200, message = "Tạo vai trò thành công", data });
        }

        [HttpPut("role")]
        public IActionResult UpdateRole([FromBody] object data)
        {
            return Ok(new { code = 200, message = "Cập nhật vai trò thành công", data });
        }

        [HttpDelete("role")]
        public IActionResult DeleteRole([FromQuery] string id)
        {
            return Ok(new { code = 200, message = "Xóa vai trò thành công" });
        }

        [HttpGet("permission")]
        public IActionResult GetPermissions()
        {
            var permissions = new[]
            {
                new { id = "p1", code = "*", name = "Toàn quyền", module = "ALL" },
                new { id = "p2", code = "SALEROOM_CASHIER_VIEW", name = "Bán hàng", module = "SALES" },
                new { id = "p3", code = "SALEROOM_CASHIER_OPEN_SHIFT", name = "Mở ca", module = "SALES" },
                new { id = "p4", code = "SALEROOM_CASHIER_CLOSE_SHIFT", name = "Đóng ca", module = "SALES" },
                new { id = "p5", code = "SOLD_OUT", name = "Báo hết món", module = "MENU" },
                new { id = "p6", code = "SYSTEM_ACCOUNT_CREATE", name = "Quản lý tài khoản", module = "SYSTEM" },
                new { id = "p7", code = "SYSTEM_ROLE_CREATE", name = "Quản lý vai trò", module = "SYSTEM" }
            };

            return Ok(new
            {
                code = 200,
                message = "Success",
                data = permissions
            });
        }

        [HttpGet("navigation")]
        public IActionResult GetNavigation()
        {
            return Ok(new
            {
                code = 200,
                message = "Success",
                data = new object[] { }
            });
        }
    }
}

