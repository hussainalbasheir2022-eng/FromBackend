using FlutterPlatform.Domain.Common;

namespace FlutterPlatform.Domain.Entities;

public class BuildLog : BaseEntity
{
    public Guid BuildId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = "Information"; // Information, Warning, Error
    public string Message { get; set; } = string.Empty;
    public string? Source { get; set; }
    
    // Navigation properties
    public Build Build { get; set; } = null!;
}
