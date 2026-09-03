using MediatR;
using FlutterPlatform.Application.Interfaces;
using FlutterPlatform.Domain.Interfaces;

namespace FlutterPlatform.Application.Commands.Auth;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IUserRepository _users;
    private readonly IJwtService _jwt;
    private readonly IPasswordHasher _hasher;

    public LoginCommandHandler(IUserRepository users, IJwtService jwt, IPasswordHasher hasher)
    {
        _users = users;
        _jwt = jwt;
        _hasher = hasher;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await _users.FindByEmailAsync(request.Email, ct);
        if (user == null || !_hasher.Verify(request.Password, user.PasswordHash))
            return new LoginResult(false, null, null, null, "Invalid credentials");

        if (!user.IsActive)
            return new LoginResult(false, null, null, null, "Account is disabled");

        var roles = await _users.GetRolesAsync(user.Id, ct);
        var accessToken = _jwt.GenerateAccessToken(user, roles);
        var refreshToken = _jwt.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(30);
        user.LastLoginAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, ct);

        return new LoginResult(true, accessToken, refreshToken, DateTime.UtcNow.AddHours(1), null);
    }
}
