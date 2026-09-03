using MediatR;
using FlutterPlatform.Application.Commands.Builds;
using FlutterPlatform.Application.Interfaces;
using FlutterPlatform.Domain.Entities;
using FlutterPlatform.Domain.Interfaces;

namespace FlutterPlatform.Application.Commands.Releases;

public record PublishProjectCommand(
    Guid ProjectId,
    Guid UserId,
    string Channel,
    bool Mandatory,
    string? ReleaseNotes
) : IRequest<CreateBuildResult>;

public class PublishProjectCommandHandler : IRequestHandler<PublishProjectCommand, CreateBuildResult>
{
    private readonly IMediator _mediator;
    private readonly IRepository<Project> _projects;

    public PublishProjectCommandHandler(IMediator mediator, IRepository<Project> projects)
    {
        _mediator = mediator;
        _projects = projects;
    }

    public async Task<CreateBuildResult> Handle(PublishProjectCommand request, CancellationToken ct)
    {
        var project = await _projects.GetByIdAsync(request.ProjectId, ct);
        if (project == null)
            return new CreateBuildResult(false, null, "Project not found");

        var result = await _mediator.Send(new CreateBuildCommand(request.ProjectId, request.UserId, request.Channel), ct);
        if (!result.Success || result.BuildId == null)
            return result;

        PublishIntentStore.Set(result.BuildId.Value, new PublishIntent(
            request.Channel,
            request.Mandatory,
            request.ReleaseNotes,
            project.ApplicationId,
            project.Version));

        return result;
    }
}
