using FlutterPlatform.Application.Commands.Releases;
using FlutterPlatform.Application.DTOs;
using FlutterPlatform.Domain.Entities;
using FlutterPlatform.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlutterPlatform.API.Controllers;

[ApiController]
[Route("api/v1/releases")]
[Authorize]
public class ReleasesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRepository<Release> _releases;
    private readonly IRepository<Build> _builds;

    public ReleasesController(IMediator mediator, IRepository<Release> releases, IRepository<Build> builds)
    {
        _mediator = mediator;
        _releases = releases;
        _builds = builds;
    }

    private Guid CurrentUserId => CurrentUser.GetId(User);

    /// <summary>List releases</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? projectId, CancellationToken ct)
    {
        var all = await _releases.GetAllAsync(ct);
        if (projectId.HasValue)
            all = all.Where(r => r.ProjectId == projectId.Value).ToList();
        return Ok(all.OrderByDescending(r => r.CreatedAt).Select(ToDto));
    }

    /// <summary>Get a release</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var r = await _releases.GetByIdAsync(id, ct);
        return r == null ? NotFound() : Ok(ToDto(r));
    }

    /// <summary>Create a release from a successful build</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReleaseRequest req, CancellationToken ct)
    {
        var build = await _builds.GetByIdAsync(req.BuildId, ct);
        if (build == null) return BadRequest(new { error = "Build not found" });

        var release = new Release
        {
            Id = Guid.NewGuid(),
            ProjectId = build.ProjectId,
            BuildId = build.Id,
            ApplicationId = req.ApplicationId,
            Version = req.Version,
            BuildNumber = build.BuildNumber,
            Channel = req.Channel ?? "production",
            Status = ReleaseStatus.Draft,
            IsMandatory = req.Mandatory,
            ReleaseNotes = req.ReleaseNotes,
            MinimumVersion = req.MinimumVersion,
            RolloutPercentage = req.RolloutPercentage ?? 100,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _releases.AddAsync(release, ct);
        return Created($"/api/v1/releases/{release.Id}", ToDto(release));
    }

    /// <summary>Publish a release to devices</summary>
    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = "SuperAdmin,Admin,ReleaseManager")]
    public async Task<IActionResult> Publish(Guid id, [FromBody] PublishReleaseRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new PublishReleaseCommand(
            id, req.Channel ?? "production", req.Mandatory, req.ReleaseNotes, CurrentUserId), ct);
        return result.Success ? Ok() : BadRequest(new { error = result.Error });
    }

    /// <summary>Rollback a published release</summary>
    [HttpPost("{id:guid}/rollback")]
    [Authorize(Roles = "SuperAdmin,Admin,ReleaseManager")]
    public async Task<IActionResult> Rollback(Guid id, [FromBody] RollbackRequest req, CancellationToken ct)
    {
        var release = await _releases.GetByIdAsync(id, ct);
        if (release == null) return NotFound();

        release.Status = ReleaseStatus.RolledBack;
        release.RollbackReason = req.Reason;
        release.UpdatedAt = DateTime.UtcNow;
        await _releases.UpdateAsync(release, ct);
        return Ok();
    }

    /// <summary>Update rollout percentage (staged deployment)</summary>
    [HttpPatch("{id:guid}/rollout")]
    public async Task<IActionResult> UpdateRollout(Guid id, [FromBody] RolloutRequest req, CancellationToken ct)
    {
        var release = await _releases.GetByIdAsync(id, ct);
        if (release == null) return NotFound();

        release.RolloutPercentage = Math.Clamp(req.Percentage, 0, 100);
        release.UpdatedAt = DateTime.UtcNow;
        await _releases.UpdateAsync(release, ct);
        return Ok();
    }

    private static ReleaseDto ToDto(Release r) => new(
        r.Id, r.ProjectId, r.BuildId, r.ApplicationId, r.Version,
        r.BuildNumber, r.Channel, r.Status.ToString(), r.IsMandatory,
        r.MinimumVersion, r.ReleaseNotes, r.PublishedAt,
        r.RolloutPercentage, r.CreatedAt);
}

public record CreateReleaseRequest(
    Guid BuildId,
    string ApplicationId,
    string Version,
    string? Channel,
    bool Mandatory,
    string? ReleaseNotes,
    string? MinimumVersion,
    int? RolloutPercentage
);
public record PublishReleaseRequest(string? Channel, bool Mandatory, string? ReleaseNotes);
public record RollbackRequest(string? Reason);
public record RolloutRequest(int Percentage);
