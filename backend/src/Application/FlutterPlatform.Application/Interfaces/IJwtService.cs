using FlutterPlatform.Domain.Entities;
using System.Security.Claims;

namespace FlutterPlatform.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user, IList<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
