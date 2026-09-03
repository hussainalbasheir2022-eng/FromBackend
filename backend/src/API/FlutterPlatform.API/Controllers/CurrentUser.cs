using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FlutterPlatform.API.Controllers;

internal static class CurrentUser
{
    public static Guid GetId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? user.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(raw) || !Guid.TryParse(raw, out var id))
            throw new InvalidOperationException("Authenticated user id is missing from the token.");

        return id;
    }
}
