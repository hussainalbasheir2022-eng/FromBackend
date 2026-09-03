using FlutterPlatform.Domain.Common;

namespace FlutterPlatform.Domain.Entities;

public enum ReleaseStatus
{
    Draft,
    Published,
    Archived,
    RolledBack
}

public class Release : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid BuildId { get; set; }
    public string ApplicationId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public int BuildNumber { get; set; }
    public string Channel { get; set; } = "production"; // development, alpha, beta, production
    public ReleaseStatus Status { get; set; } = ReleaseStatus.Draft;
    public bool IsMandatory { get; set; } = false;
    public bool Mandatory { get => IsMandatory; set => IsMandatory = value; }
    public string? MinimumVersion { get; set; }
    public string? ReleaseNotes { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? RollbackReason { get; set; }
    public int RolloutPercentage { get; set; } = 100;
    
    // Navigation properties
    public Project Project { get; set; } = null!;
    public Build Build { get; set; } = null!;
    public ReleaseManifest? Manifest { get; set; }
    public ICollection<Deployment> Deployments { get; set; } = new List<Deployment>();
}
