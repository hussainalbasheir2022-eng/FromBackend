using FlutterPlatform.Domain.Entities;
using FlutterPlatform.Domain.Interfaces;
using FlutterPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlutterPlatform.Infrastructure.Repositories;

public class DeviceRepository : IDeviceRepository
{
    private readonly AppDbContext _ctx;
    public DeviceRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task<Device?> FindByIdentifierAsync(string deviceIdentifier, CancellationToken ct = default)
        => await _ctx.Devices.FirstOrDefaultAsync(d => d.DeviceIdentifier == deviceIdentifier, ct);

    public async Task<IList<Device>> GetAllAsync(CancellationToken ct = default)
        => await _ctx.Devices.ToListAsync(ct);

    public async Task<IList<Device>> GetByChannelAsync(string channel, CancellationToken ct = default)
        => await _ctx.Devices.Where(d => d.UpdateChannel == channel).ToListAsync(ct);

    public async Task<IList<Device>> GetByApplicationAsync(string applicationId, CancellationToken ct = default)
        => await _ctx.Devices.Where(d => d.ApplicationId == applicationId).ToListAsync(ct);

    public async Task AddAsync(Device entity, CancellationToken ct = default)
    {
        await _ctx.Devices.AddAsync(entity, ct);
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Device entity, CancellationToken ct = default)
    {
        _ctx.Devices.Update(entity);
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Device entity, CancellationToken ct = default)
    {
        _ctx.Devices.Remove(entity);
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(string deviceIdentifier, CancellationToken ct = default)
        => await _ctx.Devices.AnyAsync(d => d.DeviceIdentifier == deviceIdentifier, ct);
}
