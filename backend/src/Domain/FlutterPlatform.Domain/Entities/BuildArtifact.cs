using FlutterPlatform.Domain.Common;

namespace FlutterPlatform.Domain.Entities;

public class BuildArtifact : BaseEntity
{
    public Guid BuildId { get; set; }
    public string Type { get; set; } = string.Empty; // apk, aab, symbols, etc.
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? Sha256 { get; set; }
    public string? Md5 { get; set; }
    public string? ContentType { get; set; }
    
    // Navigation properties
    public Build Build { get; set; } = null!;
}
