using System.Security.Claims;

namespace api.Extensions
{
    public static class ClaimsExtensions
    {
        public static string GetUserId(this ClaimsPrincipal user)
        {
            return user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("nameid")
                ?? throw new UnauthorizedAccessException("User id claim not found.");
        }

        public static string GetUsername(this ClaimsPrincipal user)
        {
            return user?.FindFirst(ClaimTypes.Name)?.Value ?? "guest";
        }
    }
}