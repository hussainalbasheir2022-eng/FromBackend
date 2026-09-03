using FlutterPlatform.Domain.Common;

namespace FlutterPlatform.Domain.Entities;

public enum DeploymentDeviceStatus
{
    Pending,
    Notified,
    Downloading,
    Verifying,
    Installing,
    Completed,
    Failed,
    Skipped
}

public class DeploymentDevice : BaseEntity
{
    public Guid DeploymentId { get; set; }
    public string DeviceIdentifier { get; set; } = string.Empty;
    public DeploymentDeviceStatus Status { get; set; } = DeploymentDeviceStatus.Pending;
    public DateTime? NotifiedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int? DownloadProgress { get; set; }
    
    // Navigation properties
    public Deployment Deployment { get; set; } = null!;
    public Device Device { get; set; } = null!;
}
