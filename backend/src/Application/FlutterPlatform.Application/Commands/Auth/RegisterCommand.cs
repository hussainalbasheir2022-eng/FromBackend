using MediatR;

namespace FlutterPlatform.Application.Commands.Auth;

public record RegisterCommand(string Email, string Username, string Password) : IRequest<RegisterResult>;
public record RegisterResult(bool Success, string? UserId, string? Error);
