using MediatR;
using FlutterPlatform.Application.DTOs;
using FlutterPlatform.Domain.Interfaces;

namespace FlutterPlatform.Application.Queries.Devices;

public record GetDevicesQuery(string? ApplicationId = null, string? Channel = null) : IRequest<IList<DeviceDto>>;

public class GetDevicesQueryHandler : IRequestHandler<GetDevicesQuery, IList<DeviceDto>>
{
    private readonly IDeviceRepository _devices;
    public GetDevicesQueryHandler(IDeviceRepository devices) => _devices = devices;

    public async Task<IList<DeviceDto>> Handle(GetDevicesQuery request, CancellationToken ct)
    {
        var all = await _devices.GetAllAsync(ct);
        var filtered = all.AsEnumerable();
        if (!string.IsNullOrEmpty(request.ApplicationId))
            filtered = filtered.Where(d => d.ApplicationId == request.ApplicationId);
        if (!string.IsNullOrEmpty(request.Channel))
            filtered = filtered.Where(d => d.UpdateChannel == request.Channel);

        return filtered.Select(d => new DeviceDto(
            d.Id, d.DeviceIdentifier, d.ApplicationId, d.DeviceName, d.Platform,
            d.OsVersion, d.AppVersion, d.UpdateChannel, d.Status.ToString(),
            d.LastSeenAt, d.DeviceModel, d.Manufacturer, d.BatteryLevel, d.NetworkType
        )).ToList();
    }
}
