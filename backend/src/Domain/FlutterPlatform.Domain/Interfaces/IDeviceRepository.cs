using FlutterPlatform.Domain.Entities;

namespace FlutterPlatform.Domain.Interfaces;

public interface IDeviceRepository
{
    Task<Device?> FindByIdentifierAsync(string deviceIdentifier, CancellationToken ct = default);
    Task<IList<Device>> GetAllAsync(CancellationToken ct = default);
    Task<IList<Device>> GetByChannelAsync(string channel, CancellationToken ct = default);
    Task<IList<Device>> GetByApplicationAsync(string applicationId, CancellationToken ct = default);
    Task AddAsync(Device entity, CancellationToken ct = default);
    Task UpdateAsync(Device entity, CancellationToken ct = default);
    Task DeleteAsync(Device entity, CancellationToken ct = default);
    Task<bool> ExistsAsync(string deviceIdentifier, CancellationToken ct = default);
}
