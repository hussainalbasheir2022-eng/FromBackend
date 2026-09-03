using MediatR;

namespace FlutterPlatform.Application.Commands.Auth;

public record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

public record LoginResult(
    bool Success,
    string? AccessToken,
    string? RefreshToken,
    DateTime? ExpiresAt,
    string? Error
);
