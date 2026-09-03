using FlutterPlatform.Domain.Entities;
using FlutterPlatform.Domain.Interfaces;
using FlutterPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlutterPlatform.Infrastructure.Repositories;

public class ProjectFileRepository : IProjectFileRepository
{
    private readonly AppDbContext _ctx;
    public ProjectFileRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task<ProjectFile?> GetByPathAsync(Guid projectId, string path, CancellationToken ct = default)
        => await _ctx.ProjectFiles.FirstOrDefaultAsync(f => f.ProjectId == projectId && f.Path == path, ct);

    public async Task<IList<ProjectFile>> GetByProjectAsync(Guid projectId, CancellationToken ct = default)
        => await _ctx.ProjectFiles.Where(f => f.ProjectId == projectId).ToListAsync(ct);

    public async Task AddAsync(ProjectFile file, CancellationToken ct = default)
    {
        await _ctx.ProjectFiles.AddAsync(file, ct);
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ProjectFile file, CancellationToken ct = default)
    {
        _ctx.ProjectFiles.Update(file);
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var file = await _ctx.ProjectFiles.FindAsync([id], ct);
        if (file != null)
        {
            _ctx.ProjectFiles.Remove(file);
            await _ctx.SaveChangesAsync(ct);
        }
    }
}
