using MediatR;

namespace FlutterPlatform.Application.Commands.Builds;

public record CreateBuildCommand(Guid ProjectId, Guid UserId, string? Channel = "production") : IRequest<CreateBuildResult>;
public record CreateBuildResult(bool Success, Guid? BuildId, string? Error);
