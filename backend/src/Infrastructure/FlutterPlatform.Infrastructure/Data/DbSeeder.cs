using FlutterPlatform.Application.Interfaces;
using FlutterPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlutterPlatform.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var ctx = services.GetRequiredService<AppDbContext>();

        // Seed roles
        var roleNames = new[] { "SuperAdmin", "Admin", "Developer", "ReleaseManager", "Viewer", "DeviceManager" };
        foreach (var name in roleNames)
        {
            if (!await ctx.Roles.AnyAsync(r => r.Name == name))
            {
                await ctx.Roles.AddAsync(new Role
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = $"{name} role",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        await ctx.SaveChangesAsync();

        // Seed default admin user
        if (!await ctx.Users.AnyAsync())
        {
        var hasher = services.GetRequiredService<IPasswordHasher>();
        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@flutter-platform.local",
            Username = "admin",
            PasswordHash = hasher.Hash("Admin@123!"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await ctx.Users.AddAsync(admin);
            await ctx.SaveChangesAsync();

            var superAdminRole = await ctx.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
            if (superAdminRole != null)
            {
                await ctx.UserRoles.AddAsync(new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = admin.Id,
                    RoleId = superAdminRole.Id,
                    CreatedAt = DateTime.UtcNow
                });
                await ctx.SaveChangesAsync();
            }
        }
    }
}
