using MediatR;

namespace FlutterPlatform.Application.Commands.Releases;

public record PublishReleaseCommand(
    Guid ReleaseId,
    string Channel,
    bool Mandatory,
    string? ReleaseNotes,
    Guid UserId
) : IRequest<PublishReleaseResult>;

public record PublishReleaseResult(bool Success, string? Error);
