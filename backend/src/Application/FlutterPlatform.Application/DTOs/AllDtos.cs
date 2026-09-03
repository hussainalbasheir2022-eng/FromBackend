namespace FlutterPlatform.Application.DTOs;

public record ProjectDto(
    Guid Id,
    string Name,
    string Description,
    string ApplicationId,
    string DisplayName,
    string Version,
    int BuildNumber,
    bool IsActive,
    string? IconUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record ProjectFileDto(
    Guid Id,
    Guid ProjectId,
    string Path,
    string Name,
    string? Content,
    long Size,
    DateTime UpdatedAt
);

public record BuildDto(
    Guid Id,
    Guid ProjectId,
    int BuildNumber,
    string Status,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? ErrorMessage,
    string? ArtifactUrl,
    string? Sha256,
    long? ArtifactSize,
    string? FlutterSdkVersion,
    string? DartSdkVersion,
    DateTime CreatedAt
);

public record ReleaseDto(
    Guid Id,
    Guid ProjectId,
    Guid BuildId,
    string ApplicationId,
    string Version,
    int BuildNumber,
    string Channel,
    string Status,
    bool IsMandatory,
    string? MinimumVersion,
    string? ReleaseNotes,
    DateTime? PublishedAt,
    int RolloutPercentage,
    DateTime CreatedAt
);

public record DeviceDto(
    Guid Id,
    string DeviceIdentifier,
    string ApplicationId,
    string DeviceName,
    string Platform,
    string OsVersion,
    string AppVersion,
    string UpdateChannel,
    string Status,
    DateTime LastSeenAt,
    string? DeviceModel,
    string? Manufacturer,
    int? BatteryLevel,
    string? NetworkType
);
