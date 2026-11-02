using System.Security.Claims;

namespace AnFood.Application.Helper
{
    public static class UserClaimExtensions
    {
        public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        public static string GetEmail(this ClaimsPrincipal user) =>
            user.FindFirst(ClaimTypes.Email)?.Value;

        public static string GetName(this ClaimsPrincipal user)
        {
            var name = user.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(name)) name = user.FindFirst("name")?.Value;
            if (string.IsNullOrEmpty(name)) name = user.FindFirst("unique_name")?.Value;
            if (string.IsNullOrEmpty(name)) name = user.FindFirst("preferred_username")?.Value;
            if (string.IsNullOrEmpty(name))
            {
                var email = user.GetEmail();
                if (!string.IsNullOrEmpty(email))
                {
                    name = email.Split('@')[0];
                }
            }
            return name ?? "system";
        }

        public static string GetRole(this ClaimsPrincipal user) =>
            user.FindFirst(ClaimTypes.Role)?.Value;


        public static string GetCode(this ClaimsPrincipal user) =>
            user.FindFirst("userCode")?.Value;
        public static string GetApplicationId(this ClaimsPrincipal user) =>
      user.FindFirst("applicationId")?.Value;


        public static UserClaimsInfo GetUserClaimsInfo(this ClaimsPrincipal user)
        {
            foreach (var claim in user.Claims)
            {
                Console.WriteLine($"Type: {claim.Type} | Value: {claim.Value}");
            }

            return new UserClaimsInfo
            {
                UserId = user.GetUserId(),
                Email = user.GetEmail(),
                Name = user.GetName(),
                Role = user.GetRole(),
                ApplicationId = user.GetApplicationId(),
                UserCode = user.GetCode()
            };
        }

        public static bool HasPermission(this ClaimsPrincipal user, string requiredPermission)
        {
            if (user.IsInRole("Admin") || string.Equals(user.GetRole(), "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.IsNullOrEmpty(requiredPermission)) return false;
            var normalizedQuery = requiredPermission.Replace(".", "_").ToUpper();

            return user.HasClaim(c => (string.Equals(c.Type, "permission", StringComparison.OrdinalIgnoreCase) || 
                                       string.Equals(c.Type, "permissions", StringComparison.OrdinalIgnoreCase)) &&
                                      c.Value != null &&
                                      string.Equals(c.Value.Replace(".", "_").ToUpper(), normalizedQuery));
        }
    }

    public class UserClaimsInfo
    {
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? Role { get; set; }
        public string? ApplicationId { get; set; }
        public string? UserCode { get; set; }
    }

}
