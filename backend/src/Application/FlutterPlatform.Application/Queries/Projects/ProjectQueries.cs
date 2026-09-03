using MediatR;
using FlutterPlatform.Application.DTOs;
using FlutterPlatform.Domain.Entities;
using FlutterPlatform.Domain.Interfaces;

namespace FlutterPlatform.Application.Queries.Projects;

// ── GetProjects ──────────────────────────────────────────────────────────────
public record GetProjectsQuery(Guid? OwnerId = null) : IRequest<IList<ProjectDto>>;

public class GetProjectsQueryHandler : IRequestHandler<GetProjectsQuery, IList<ProjectDto>>
{
    private readonly IRepository<Project> _projects;
    public GetProjectsQueryHandler(IRepository<Project> projects) => _projects = projects;

    public async Task<IList<ProjectDto>> Handle(GetProjectsQuery request, CancellationToken ct)
    {
        var all = await _projects.GetAllAsync(ct);
        var filtered = request.OwnerId.HasValue
            ? all.Where(p => p.OwnerId == request.OwnerId.Value)
            : all.AsEnumerable();

        return filtered.Select(ToDto).ToList();
    }

    internal static ProjectDto ToDto(Project p) => new(
        p.Id, p.Name, p.Description, p.ApplicationId,
        p.DisplayName, p.Version, p.BuildNumber, p.IsActive,
        p.IconUrl, p.CreatedAt, p.UpdatedAt);
}

// ── GetProjectById ────────────────────────────────────────────────────────────
public record GetProjectByIdQuery(Guid ProjectId) : IRequest<ProjectDto?>;

public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, ProjectDto?>
{
    private readonly IRepository<Project> _projects;
    public GetProjectByIdQueryHandler(IRepository<Project> projects) => _projects = projects;

    public async Task<ProjectDto?> Handle(GetProjectByIdQuery request, CancellationToken ct)
    {
        var p = await _projects.GetByIdAsync(request.ProjectId, ct);
        return p == null ? null : GetProjectsQueryHandler.ToDto(p);
    }
}
