using MediatR;
using FlutterPlatform.Application.Interfaces;
using FlutterPlatform.Domain.Entities;
using FlutterPlatform.Domain.Interfaces;

namespace FlutterPlatform.Application.Commands.Auth;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;

    public RegisterCommandHandler(IUserRepository users, IPasswordHasher hasher)
    {
        _users = users;
        _hasher = hasher;
    }

    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken ct)
    {
        if (await _users.EmailExistsAsync(request.Email, ct))
            return new RegisterResult(false, null, "Email already in use");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.ToLower(),
            Username = request.Username,
            PasswordHash = _hasher.Hash(request.Password),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _users.AddAsync(user, ct);

        // Assign default Developer role
        await _users.AssignRoleAsync(user.Id, "Developer", ct);

        return new RegisterResult(true, user.Id.ToString(), null);
    }
}
