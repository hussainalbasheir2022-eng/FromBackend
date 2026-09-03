using FlutterPlatform.Domain.Common;

namespace FlutterPlatform.Domain.Entities;

public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ApplicationId { get; set; } = string.Empty; // Android package name
    public string DisplayName { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public int BuildNumber { get; set; } = 1;
    public Guid OwnerId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? IconUrl { get; set; }
    public string? SplashImageUrl { get; set; }
    
    // Navigation properties
    public User Owner { get; set; } = null!;
    public ICollection<ProjectFile> Files { get; set; } = new List<ProjectFile>();
    public ICollection<ProjectVersion> Versions { get; set; } = new List<ProjectVersion>();
    public ICollection<Build> Builds { get; set; } = new List<Build>();
    public ICollection<Release> Releases { get; set; } = new List<Release>();
}
