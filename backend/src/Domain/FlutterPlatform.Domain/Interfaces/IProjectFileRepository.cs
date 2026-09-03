using FlutterPlatform.Domain.Entities;

namespace FlutterPlatform.Domain.Interfaces;

public interface IProjectFileRepository
{
    Task<ProjectFile?> GetByPathAsync(Guid projectId, string path, CancellationToken ct = default);
    Task<IList<ProjectFile>> GetByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task AddAsync(ProjectFile file, CancellationToken ct = default);
    Task UpdateAsync(ProjectFile file, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
