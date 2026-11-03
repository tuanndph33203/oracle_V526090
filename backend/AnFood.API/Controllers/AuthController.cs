using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AnFood.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace AnFood.API.Controllers
{
    [ApiController]
    [Route("api")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;

        public AuthController(IConfiguration config)
        {
            _config = config;
        }

        [HttpPost("authentication/login")]
        [AllowAnonymous]
        public IActionResult Login([FromBody] LoginRequestModel model)
        {
            var email = string.IsNullOrWhiteSpace(model.Email) ? "admin@utc.edu.vn" : model.Email.Trim();
            var name = email.Contains('@') ? email.Split('@')[0] : email;
            if (name.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                name = "Quản trị viên";
            }

            var userId = "11111111-1111-1111-1111-111111111111";
            var userCode = "ADMIN";
            var role = "Admin";
            var appId = "UTC_COFFEE";

            var token = GenerateToken(userId, email, name, role, userCode, appId);

            var responseData = new
            {
                accessToken = token,
                refreshToken = "ref_" + Guid.NewGuid().ToString("N"),
                accessTokenExpiresAt = DateTime.UtcNow.AddDays(30).ToString("o"),
                refreshTokenExpiresAt = DateTime.UtcNow.AddDays(60).ToString("o"),
                isSuccess = true,
                message = "Đăng nhập thành công",
                userId = userId,
                userName = name,
                email = email,
                code = userCode,
                firstName = "Quản trị",
                lastName = "Viên",
                roles = new[]
                {
                    new { roleId = "admin-role", roleName = "Quản trị viên", roleCode = "ADMIN" }
                },
                permissions = new[] { "*", "ADMIN", "SALEROOM_CASHIER_ADMIN" },
                applyBranchCodes = new[] { "02" }
            };

            return Ok(new
            {
                code = 200,
                message = "Đăng nhập thành công",
                data = responseData
            });
        }

        [HttpGet("account/get-info")]
        [Authorize]
        public IActionResult GetInfo()
        {
            var user = HttpContext.User;
            var email = user.FindFirst(ClaimTypes.Email)?.Value ?? "admin@utc.edu.vn";
            var name = user.FindFirst(ClaimTypes.Name)?.Value ?? (email.Contains('@') ? email.Split('@')[0] : email);
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "11111111-1111-1111-1111-111111111111";
            var userCode = user.FindFirst("userCode")?.Value ?? "ADMIN";

            return Ok(new
            {
                code = 200,
                message = "Success",
                data = new
                {
                    userId = userId,
                    userName = name,
                    email = email,
                    code = userCode,
                    firstName = "Quản trị",
                    lastName = "Viên",
                    roles = new[]
                    {
                        new { roleId = "admin-role", roleName = "Quản trị viên", roleCode = "ADMIN" }
                    },
                    permissions = new[] { "*", "ADMIN", "SALEROOM_CASHIER_ADMIN" },
                    applyBranchCodes = new[] { "02" }
                }
            });
        }

        [HttpGet("account/get-roles")]
        [Authorize]
        public IActionResult GetRoles([FromQuery] string? userId)
        {
            return Ok(new
            {
                code = 200,
                message = "Success",
                data = new[]
                {
                    new { id = "admin-role", name = "Quản trị viên", code = "ADMIN" }
                }
            });
        }

        [HttpGet("authentication/refreshToken")]
        public IActionResult RefreshToken()
        {
            var token = GenerateToken("11111111-1111-1111-1111-111111111111", "admin@utc.edu.vn", "Quản trị viên", "Admin", "ADMIN", "UTC_COFFEE");
            return Ok(new
            {
                code = 200,
                data = new
                {
                    accessToken = token,
                    refreshToken = "ref_" + Guid.NewGuid().ToString("N")
                }
            });
        }

        [HttpGet("authentication/issue-token")]
        [AllowAnonymous]
        public IActionResult IssueToken([FromQuery] string? sectionId)
        {
            var token = GenerateToken("11111111-1111-1111-1111-111111111111", "admin@utc.edu.vn", "Quản trị viên", "Admin", "ADMIN", "UTC_COFFEE");
            return Ok(new
            {
                code = 200,
                data = new
                {
                    accessToken = token,
                    refreshToken = "ref_" + Guid.NewGuid().ToString("N")
                }
            });
        }

        private string GenerateToken(string userId, string email, string name, string role, string userCode, string appId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var keyString = _config["Jwt:Key"] ?? "e7+gFLUw7oVcQmpXubQ0zHK5r1H1dN2eN5pgX/o/6F3gJ2MycPvOAiFsdDRDaIRn7WQnArW0KNkxLQa0QKBIxQ==";
            var key = Encoding.UTF8.GetBytes(keyString);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Role, role),
                new Claim("userCode", userCode),
                new Claim("applicationId", appId),
                new Claim("applyBranchCodes", "02")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(30),
                Issuer = _config["Jwt:Issuer"] ?? "https://utc-coffee.edu.vn",
                Audience = _config["Jwt:Audience"] ?? "User",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    public class LoginRequestModel
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? ApplicationId { get; set; }
    }
}

