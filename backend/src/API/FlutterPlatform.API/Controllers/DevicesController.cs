using FlutterPlatform.Application.Commands.Devices;
using FlutterPlatform.Application.Queries.Devices;
using FlutterPlatform.Domain.Entities;
using FlutterPlatform.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlutterPlatform.API.Controllers;

[ApiController]
[Route("api/v1/devices")]
public class DevicesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IDeviceRepository _devices;

    public DevicesController(IMediator mediator, IDeviceRepository devices)
    {
        _mediator = mediator;
        _devices = devices;
    }

    /// <summary>List all devices (dashboard)</summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? applicationId,
        [FromQuery] string? channel,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDevicesQuery(applicationId, channel), ct);
        return Ok(result);
    }

    /// <summary>Get a single device</summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var all = await _devices.GetAllAsync(ct);
        var device = all.FirstOrDefault(d => d.Id == id);
        return device == null ? NotFound() : Ok(device);
    }

    /// <summary>Register a device (called from Android app)</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDeviceRequest req, CancellationToken ct)
    {
        var existing = await _devices.FindByIdentifierAsync(req.DeviceIdentifier, ct);
        if (existing != null)
        {
            existing.DeviceName = req.DeviceName;
            existing.ApplicationId = req.ApplicationId;
            existing.OsVersion = req.OsVersion;
            existing.AppVersion = req.AppVersion;
            existing.UpdateChannel = req.UpdateChannel ?? "production";
            existing.DeviceModel = req.DeviceModel;
            existing.Manufacturer = req.Manufacturer;
            existing.LastSeenAt = DateTime.UtcNow;
            existing.Status = DeviceStatus.Online;
            await _devices.UpdateAsync(existing, ct);
            return Ok(new { deviceId = existing.Id, registered = false });
        }

        var device = new Device
        {
            Id = Guid.NewGuid(),
            DeviceIdentifier = req.DeviceIdentifier,
            ApplicationId = req.ApplicationId,
            DeviceName = req.DeviceName,
            Platform = "android",
            OsVersion = req.OsVersion,
            AppVersion = req.AppVersion,
            UpdateChannel = req.UpdateChannel ?? "production",
            DeviceModel = req.DeviceModel,
            Manufacturer = req.Manufacturer,
            Status = DeviceStatus.Online,
            LastSeenAt = DateTime.UtcNow,
            RegisteredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _devices.AddAsync(device, ct);
        return Created($"/api/v1/devices/{device.Id}", new { deviceId = device.Id, registered = true });
    }

    /// <summary>Heartbeat from device (updates status and version)</summary>
    [AllowAnonymous]
    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat([FromBody] HeartbeatRequest req, CancellationToken ct)
    {
        await _mediator.Send(new UpdateDeviceHeartbeatCommand(
            req.DeviceIdentifier, req.AppVersion, req.BatteryLevel, req.NetworkType), ct);
        return Ok(new { timestamp = DateTime.UtcNow });
    }

    /// <summary>Report update status from device</summary>
    [HttpPost("{deviceIdentifier}/update-status")]
    public async Task<IActionResult> UpdateStatus(
        string deviceIdentifier,
        [FromBody] UpdateStatusRequest req,
        CancellationToken ct)
    {
        var device = await _devices.FindByIdentifierAsync(deviceIdentifier, ct);
        if (device == null) return NotFound();
        // Status is just logged; dashboard uses SignalR for real-time view
        return Ok();
    }
}

public record RegisterDeviceRequest(
    string DeviceIdentifier,
    string ApplicationId,
    string DeviceName,
    string OsVersion,
    string AppVersion,
    string? UpdateChannel,
    string? DeviceModel,
    string? Manufacturer
);

public record HeartbeatRequest(
    string DeviceIdentifier,
    string AppVersion,
    int? BatteryLevel,
    string? NetworkType
);

public record UpdateStatusRequest(string Status, int? Progress, string? Error);
