using FlutterPlatform.Domain.Entities;
using FlutterPlatform.Domain.Interfaces;
using FlutterPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlutterPlatform.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _ctx;
    public UserRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
        => await _ctx.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower(), ct);

    public async Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => await _ctx.Users.FindAsync([id], ct);

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
        => await _ctx.Users.AnyAsync(u => u.Email == email.ToLower(), ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await _ctx.Users.AddAsync(user, ct);
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _ctx.Users.Update(user);
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task<IList<string>> GetRolesAsync(Guid userId, CancellationToken ct = default)
        => await _ctx.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .ToListAsync(ct);

    public async Task AssignRoleAsync(Guid userId, string roleName, CancellationToken ct = default)
    {
        var role = await _ctx.Roles.FirstOrDefaultAsync(r => r.Name == roleName, ct);
        if (role == null)
        {
            role = new Role { Id = Guid.NewGuid(), Name = roleName, CreatedAt = DateTime.UtcNow };
            await _ctx.Roles.AddAsync(role, ct);
        }
        var existing = await _ctx.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == role.Id, ct);
        if (!existing)
        {
            await _ctx.UserRoles.AddAsync(new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RoleId = role.Id,
                CreatedAt = DateTime.UtcNow
            }, ct);
        }
        await _ctx.SaveChangesAsync(ct);
    }
}
