using MediatR;

namespace FlutterPlatform.Application.Commands.Files;

public record UpsertProjectFileCommand(
    Guid ProjectId,
    string Path,
    string Content,
    Guid UserId
) : IRequest<UpsertFileResult>;

public record UpsertFileResult(bool Success, string? Error);
