using FlutterPlatform.Domain.Common;

namespace FlutterPlatform.Domain.Entities;

public enum UpdateState
{
    Idle,
    Checking,
    Available,
    Downloading,
    Downloaded,
    Verifying,
    Installing,
    Restarting,
    Healthy,
    Failed,
    Rollback
}

public class DeviceInstallation : BaseEntity
{
    public string DeviceIdentifier { get; set; } = string.Empty;
    public Guid ReleaseId { get; set; }
    public string Version { get; set; } = string.Empty;
    public int BuildNumber { get; set; }
    public UpdateState State { get; set; } = UpdateState.Idle;
    public DateTime? InstalledAt { get; set; }
    public DateTime? LastHealthCheckAt { get; set; }
    public bool IsHealthy { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public int? DownloadProgress { get; set; }
    public string? RollbackVersion { get; set; }
    
    // Navigation properties
    public Device Device { get; set; } = null!;
    public Release Release { get; set; } = null!;
}
