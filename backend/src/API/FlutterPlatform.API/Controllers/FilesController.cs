using FlutterPlatform.Application.Commands.Files;
using FlutterPlatform.Application.DTOs;
using FlutterPlatform.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlutterPlatform.API.Controllers;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/files")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IProjectFileRepository _files;

    public FilesController(IMediator mediator, IProjectFileRepository files)
    {
        _mediator = mediator;
        _files = files;
    }

    private Guid CurrentUserId => CurrentUser.GetId(User);

    /// <summary>List all files for a project (metadata only, no content)</summary>
    [HttpGet]
    public async Task<IActionResult> List(Guid projectId, CancellationToken ct)
    {
        var files = await _files.GetByProjectAsync(projectId, ct);
        var dtos = files.Select(f => new ProjectFileDto(
            f.Id, f.ProjectId, f.Path, f.Name, null, f.Size, f.UpdatedAt));
        return Ok(dtos);
    }

    /// <summary>Get file content</summary>
    [HttpGet("{**path}")]
    public async Task<IActionResult> Get(Guid projectId, string path, CancellationToken ct)
    {
        var file = await _files.GetByPathAsync(projectId, path, ct);
        if (file == null) return NotFound();
        return Ok(new ProjectFileDto(
            file.Id, file.ProjectId, file.Path, file.Name,
            file.Content, file.Size, file.UpdatedAt));
    }

    /// <summary>Create or update a file</summary>
    [HttpPut("{**path}")]
    public async Task<IActionResult> Upsert(
        Guid projectId, string path,
        [FromBody] UpsertFileRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new UpsertProjectFileCommand(projectId, path, req.Content, CurrentUserId), ct);
        return result.Success ? Ok() : BadRequest(new { error = result.Error });
    }

    /// <summary>Delete a file</summary>
    [HttpDelete("{**path}")]
    public async Task<IActionResult> Delete(Guid projectId, string path, CancellationToken ct)
    {
        var file = await _files.GetByPathAsync(projectId, path, ct);
        if (file == null) return NotFound();
        await _files.DeleteAsync(file.Id, ct);
        return NoContent();
    }
}

public record UpsertFileRequest(string Content);
