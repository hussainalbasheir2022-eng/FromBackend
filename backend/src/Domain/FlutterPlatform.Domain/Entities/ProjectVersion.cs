using FlutterPlatform.Domain.Common;

namespace FlutterPlatform.Domain.Entities;

public class ProjectVersion : BaseEntity
{
    public Guid ProjectId { get; set; }
    public string Version { get; set; } = string.Empty;
    public int BuildNumber { get; set; }
    public string? Branch { get; set; }
    public string? CommitHash { get; set; }
    public string? CommitMessage { get; set; }
    public bool IsReleased { get; set; } = false;
    public string? ReleaseNotes { get; set; }
    
    // Navigation properties
    public Project Project { get; set; } = null!;
    public ICollection<ProjectFile> Files { get; set; } = new List<ProjectFile>();
    public ICollection<Build> Builds { get; set; } = new List<Build>();
}
