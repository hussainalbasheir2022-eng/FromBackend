using FlutterPlatform.Domain.Common;

namespace FlutterPlatform.Domain.Entities;

public enum DeploymentStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

public class Deployment : BaseEntity
{
    public Guid ReleaseId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public DeploymentStatus Status { get; set; } = DeploymentStatus.Pending;
    public int RolloutPercentage { get; set; } = 100;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int TotalDevices { get; set; }
    public int CompletedDevices { get; set; }
    public int FailedDevices { get; set; }
    public int PendingDevices { get; set; }
    
    // Navigation properties
    public Release Release { get; set; } = null!;
    public ICollection<DeploymentDevice> DeploymentDevices { get; set; } = new List<DeploymentDevice>();
}
