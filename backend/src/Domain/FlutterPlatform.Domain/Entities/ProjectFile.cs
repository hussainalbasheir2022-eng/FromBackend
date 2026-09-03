using FlutterPlatform.Domain.Common;

namespace FlutterPlatform.Domain.Entities;

public class ProjectFile : BaseEntity
{
    public Guid ProjectId { get; set; }
    public string Path { get; set; } = string.Empty; // Relative path in project
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/plain";
    public long Size { get; set; }
    public string? Sha256 { get; set; }
    
    // Navigation properties
    public Project Project { get; set; } = null!;
}
