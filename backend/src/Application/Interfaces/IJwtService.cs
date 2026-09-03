using FlutterPlatform.Domain.Entities;

namespace FlutterPlatform.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user, IList<string> roles);
    string GenerateRefreshToken();
    System.Security.Claims.ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
