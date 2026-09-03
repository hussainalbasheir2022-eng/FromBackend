using FlutterPlatform.Domain.Common;

namespace FlutterPlatform.Domain.Entities;

public class DeviceGroup : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Channel { get; set; } = "production"; // development, testing, production
    
    // Navigation properties
    public ICollection<DeviceGroupMembership> DeviceGroupMemberships { get; set; } = new List<DeviceGroupMembership>();
}
