using FlutterPlatform.Domain.Common;

namespace FlutterPlatform.Domain.Entities;

public class UserRole : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
