using MediatR;
using FlutterPlatform.Domain.Entities;
using FlutterPlatform.Domain.Interfaces;

namespace FlutterPlatform.Application.Commands.Files;

public class UpsertProjectFileCommandHandler : IRequestHandler<UpsertProjectFileCommand, UpsertFileResult>
{
    private readonly IProjectFileRepository _files;
    private readonly IRepository<Project> _projects;

    public UpsertProjectFileCommandHandler(IProjectFileRepository files, IRepository<Project> projects)
    {
        _files = files;
        _projects = projects;
    }

    public async Task<UpsertFileResult> Handle(UpsertProjectFileCommand request, CancellationToken ct)
    {
        var project = await _projects.GetByIdAsync(request.ProjectId, ct);
        if (project == null) return new UpsertFileResult(false, "Project not found");

        var existing = await _files.GetByPathAsync(request.ProjectId, request.Path, ct);
        if (existing != null)
        {
            existing.Content = request.Content;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.Size = System.Text.Encoding.UTF8.GetByteCount(request.Content);
            await _files.UpdateAsync(existing, ct);
        }
        else
        {
            var file = new ProjectFile
            {
                Id = Guid.NewGuid(),
                ProjectId = request.ProjectId,
                Path = request.Path,
                Name = System.IO.Path.GetFileName(request.Path),
                Content = request.Content,
                Size = System.Text.Encoding.UTF8.GetByteCount(request.Content),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _files.AddAsync(file, ct);
        }

        return new UpsertFileResult(true, null);
    }
}
