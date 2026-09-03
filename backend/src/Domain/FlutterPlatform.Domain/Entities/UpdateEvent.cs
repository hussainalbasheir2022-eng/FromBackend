using FlutterPlatform.Domain.Common;

namespace FlutterPlatform.Domain.Entities;

public enum UpdateEventType
{
    Available,
    DownloadStarted,
    DownloadCompleted,
    VerificationStarted,
    VerificationCompleted,
    InstallationStarted,
    InstallationCompleted,
    AppRestarted,
    HealthCheck,
    Failed,
    RollbackInitiated,
    RollbackCompleted
}

public class UpdateEvent : BaseEntity
{
    public string DeviceIdentifier { get; set; } = string.Empty;
    public Guid ReleaseId { get; set; }
    public UpdateEventType EventType { get; set; }
    public string? Message { get; set; }
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
    
    // Navigation properties
    public Device Device { get; set; } = null!;
    public Release Release { get; set; } = null!;
}
