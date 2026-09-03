using MediatR;

namespace FlutterPlatform.Application.Commands.Devices;

public record UpdateDeviceHeartbeatCommand(
    string DeviceIdentifier,
    string AppVersion,
    int? BatteryLevel,
    string? NetworkType
) : IRequest;

public class UpdateDeviceHeartbeatCommandHandler : IRequestHandler<UpdateDeviceHeartbeatCommand>
{
    private readonly FlutterPlatform.Domain.Interfaces.IDeviceRepository _devices;
    private readonly FlutterPlatform.Application.Interfaces.ISignalRNotifier _notifier;

    public UpdateDeviceHeartbeatCommandHandler(
        FlutterPlatform.Domain.Interfaces.IDeviceRepository devices,
        FlutterPlatform.Application.Interfaces.ISignalRNotifier notifier)
    {
        _devices = devices;
        _notifier = notifier;
    }

    public async Task Handle(UpdateDeviceHeartbeatCommand request, CancellationToken ct)
    {
        var device = await _devices.FindByIdentifierAsync(request.DeviceIdentifier, ct);
        if (device == null) return;

        bool versionChanged = device.AppVersion != request.AppVersion;
        device.AppVersion = request.AppVersion;
        device.BatteryLevel = request.BatteryLevel;
        device.NetworkType = request.NetworkType;
        device.LastSeenAt = DateTime.UtcNow;
        device.Status = FlutterPlatform.Domain.Entities.DeviceStatus.Online;
        await _devices.UpdateAsync(device, ct);

        if (versionChanged)
            await _notifier.NotifyDeviceVersionChanged(request.DeviceIdentifier, request.AppVersion);
    }
}
