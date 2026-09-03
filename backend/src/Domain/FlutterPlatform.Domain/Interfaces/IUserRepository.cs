using FlutterPlatform.Domain.Entities;

namespace FlutterPlatform.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task<IList<string>> GetRolesAsync(Guid userId, CancellationToken ct = default);
    Task AssignRoleAsync(Guid userId, string roleName, CancellationToken ct = default);
}
