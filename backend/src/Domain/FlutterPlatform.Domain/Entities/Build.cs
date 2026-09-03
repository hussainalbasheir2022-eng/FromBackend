using FlutterPlatform.Domain.Common;

namespace FlutterPlatform.Domain.Entities;

public enum BuildStatus
{
    Pending,
    Queued,
    Running,
    Succeeded,
    Failed,
    Cancelled,
    Timeout
}

public class Build : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid ProjectVersionId { get; set; }
    public int BuildNumber { get; set; }
    public BuildStatus Status { get; set; } = BuildStatus.Pending;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? WorkerId { get; set; }
    public string? FlutterSdkVersion { get; set; }
    public string? DartSdkVersion { get; set; }
    public string? AndroidSdkVersion { get; set; }
    public string? GradleVersion { get; set; }
    public string? Sha256 { get; set; }
    public long? ArtifactSize { get; set; }
    public string? ArtifactUrl { get; set; }
    
    // Navigation properties
    public Project Project { get; set; } = null!;
    public ProjectVersion ProjectVersion { get; set; } = null!;
    public ICollection<BuildLog> Logs { get; set; } = new List<BuildLog>();
    public ICollection<BuildArtifact> Artifacts { get; set; } = new List<BuildArtifact>();
}
