using FlutterPlatform.Domain.Common;

namespace FlutterPlatform.Domain.Entities;

public class DeviceGroupMembership : BaseEntity
{
    public string DeviceIdentifier { get; set; } = string.Empty;
    public Guid DeviceGroupId { get; set; }
    
    // Navigation properties
    public Device Device { get; set; } = null!;
    public DeviceGroup DeviceGroup { get; set; } = null!;
}
