using FlutterPlatform.Domain.Common;

namespace FlutterPlatform.Domain.Entities;

public class ReleaseManifest : BaseEntity
{
    public Guid ReleaseId { get; set; }
    public string ApplicationId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public int BuildNumber { get; set; }
    public string? MinimumVersion { get; set; }
    public string ArtifactUrl { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public bool IsMandatory { get; set; } = false;
    
    // Navigation properties
    public Release Release { get; set; } = null!;
}
