using FlutterPlatform.Domain.Common;

namespace FlutterPlatform.Domain.Entities;

public enum DeviceStatus
{
    Online,
    Offline,
    Unhealthy
}

public class Device : BaseEntity
{
    public string DeviceIdentifier { get; set; } = string.Empty; // Unique device identifier from Android
    public string ApplicationId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Platform { get; set; } = "android";
    public string OsVersion { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    public string UpdateChannel { get; set; } = "production";
    public DeviceStatus Status { get; set; } = DeviceStatus.Offline;
    public DateTime LastSeenAt { get; set; }
    public DateTime? RegisteredAt { get; set; }
    public string? DeviceModel { get; set; }
    public string? Manufacturer { get; set; }
    public int? BatteryLevel { get; set; }
    public string? NetworkType { get; set; }
    
    // Navigation properties
    public ICollection<DeviceGroupMembership> DeviceGroupMemberships { get; set; } = new List<DeviceGroupMembership>();
    public ICollection<DeviceInstallation> Installations { get; set; } = new List<DeviceInstallation>();
    public ICollection<DeploymentDevice> DeploymentDevices { get; set; } = new List<DeploymentDevice>();
    public ICollection<UpdateEvent> UpdateEvents { get; set; } = new List<UpdateEvent>();
}
