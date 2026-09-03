using MediatR;
using FlutterPlatform.Application.Interfaces;
using FlutterPlatform.Domain.Entities;
using FlutterPlatform.Domain.Interfaces;

namespace FlutterPlatform.Application.Commands.Builds;

public class CreateBuildCommandHandler : IRequestHandler<CreateBuildCommand, CreateBuildResult>
{
    private readonly IRepository<Project> _projects;
    private readonly IRepository<Build> _builds;
    private readonly IRepository<ProjectVersion> _versions;
    private readonly IBuildQueue _queue;

    public CreateBuildCommandHandler(
        IRepository<Project> projects,
        IRepository<Build> builds,
        IRepository<ProjectVersion> versions,
        IBuildQueue queue)
    {
        _projects = projects;
        _builds = builds;
        _versions = versions;
        _queue = queue;
    }

    public async Task<CreateBuildResult> Handle(CreateBuildCommand request, CancellationToken ct)
    {
        var project = await _projects.GetByIdAsync(request.ProjectId, ct);
        if (project == null) return new CreateBuildResult(false, null, "Project not found");

        // Create a project version snapshot using the next build number
        project.BuildNumber++;
        var version = new ProjectVersion
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Version = project.Version,
            BuildNumber = project.BuildNumber,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.UserId.ToString()
        };
        await _versions.AddAsync(version, ct);

        await _projects.UpdateAsync(project, ct);

        var build = new Build
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            ProjectVersionId = version.Id,
            BuildNumber = version.BuildNumber,
            Status = BuildStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        await _builds.AddAsync(build, ct);

        await _queue.EnqueueAsync(build.Id, ct);

        return new CreateBuildResult(true, build.Id, null);
    }
}
