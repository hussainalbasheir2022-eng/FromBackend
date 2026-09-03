using FlutterPlatform.Application.DTOs;
using FlutterPlatform.Domain.Entities;
using FlutterPlatform.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlutterPlatform.API.Controllers;

[ApiController]
[Route("api/v1/builds")]
[Authorize]
public class BuildsController : ControllerBase
{
    private readonly IRepository<Build> _builds;
    private readonly IRepository<BuildLog> _logs;

    public BuildsController(IRepository<Build> builds, IRepository<BuildLog> logs)
    {
        _builds = builds;
        _logs = logs;
    }

    /// <summary>List all builds (optionally filter by project)</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? projectId, CancellationToken ct)
    {
        var all = await _builds.GetAllAsync(ct);
        if (projectId.HasValue)
            all = all.Where(b => b.ProjectId == projectId.Value).ToList();

        return Ok(all.OrderByDescending(b => b.CreatedAt).Select(ToDto));
    }

    /// <summary>Get a specific build</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var build = await _builds.GetByIdAsync(id, ct);
        return build == null ? NotFound() : Ok(ToDto(build));
    }

    /// <summary>Get build logs</summary>
    [HttpGet("{id:guid}/logs")]
    public async Task<IActionResult> GetLogs(Guid id, CancellationToken ct)
    {
        var logs = await _logs.FindAsync(l => l.BuildId == id, ct);
        return Ok(logs.OrderBy(l => l.Timestamp).Select(l => new
        {
            l.Id,
            l.Level,
            l.Message,
            l.Timestamp
        }));
    }

    /// <summary>Cancel a running build</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var build = await _builds.GetByIdAsync(id, ct);
        if (build == null) return NotFound();
        if (build.Status is not (BuildStatus.Pending or BuildStatus.Queued or BuildStatus.Running))
            return BadRequest(new { error = "Build cannot be cancelled in current state" });

        build.Status = BuildStatus.Cancelled;
        build.CompletedAt = DateTime.UtcNow;
        await _builds.UpdateAsync(build, ct);
        return Ok();
    }

    private static BuildDto ToDto(Build b) => new(
        b.Id, b.ProjectId, b.BuildNumber, b.Status.ToString(),
        b.StartedAt, b.CompletedAt, b.ErrorMessage, b.ArtifactUrl,
        b.Sha256, b.ArtifactSize, b.FlutterSdkVersion, b.DartSdkVersion,
        b.CreatedAt);
}
