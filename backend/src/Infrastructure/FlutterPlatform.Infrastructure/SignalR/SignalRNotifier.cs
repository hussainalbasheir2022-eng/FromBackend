using FlutterPlatform.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace FlutterPlatform.Infrastructure.SignalR;

public class SignalRNotifier : ISignalRNotifier
{
    private readonly IHubContext<BuildHub> _buildHub;
    private readonly IHubContext<DeploymentHub> _deploymentHub;

    public SignalRNotifier(IHubContext<BuildHub> buildHub, IHubContext<DeploymentHub> deploymentHub)
    {
        _buildHub = buildHub;
        _deploymentHub = deploymentHub;
    }

    public async Task NotifyBuildStarted(Guid buildId, string projectName)
        => await _buildHub.Clients.All
            .SendAsync("build.started", new { buildId, projectName, timestamp = DateTime.UtcNow });

    public async Task NotifyBuildLog(Guid buildId, string message, string level = "info")
        => await _buildHub.Clients.All
            .SendAsync("build.log", new { buildId, message, level, timestamp = DateTime.UtcNow });

    public async Task NotifyBuildCompleted(Guid buildId, bool success, string? artifactUrl = null)
        => await _buildHub.Clients.All
            .SendAsync("build.completed", new { buildId, success, artifactUrl, timestamp = DateTime.UtcNow });

    public async Task NotifyBuildFailed(Guid buildId, string error)
        => await _buildHub.Clients.All
            .SendAsync("build.failed", new { buildId, error, timestamp = DateTime.UtcNow });

    public async Task NotifyReleasePublished(Guid releaseId, string applicationId, string version, bool mandatory)
    {
        var payload = new { releaseId, applicationId, version, mandatory, timestamp = DateTime.UtcNow };
        await _deploymentHub.Clients.Group($"app-{applicationId}")
            .SendAsync("deployment.available", payload);
        await _deploymentHub.Clients.All
            .SendAsync("deployment.available", payload);
    }

    public async Task NotifyDeploymentStarted(Guid deploymentId, Guid releaseId)
        => await _deploymentHub.Clients.All
            .SendAsync("deployment.started", new { deploymentId, releaseId, timestamp = DateTime.UtcNow });

    public async Task NotifyDeploymentProgress(Guid deploymentId, string deviceId, string status, int? progressPercent)
        => await _deploymentHub.Clients.Group($"device-{deviceId}")
            .SendAsync("deployment.progress", new { deploymentId, deviceId, status, progressPercent, timestamp = DateTime.UtcNow });

    public async Task NotifyDeploymentCompleted(Guid deploymentId)
        => await _deploymentHub.Clients.All
            .SendAsync("deployment.completed", new { deploymentId, timestamp = DateTime.UtcNow });

    public async Task NotifyDeviceOnline(string deviceId, string appVersion)
        => await _deploymentHub.Clients.All
            .SendAsync("device.online", new { deviceId, appVersion, timestamp = DateTime.UtcNow });

    public async Task NotifyDeviceOffline(string deviceId)
        => await _deploymentHub.Clients.All
            .SendAsync("device.offline", new { deviceId, timestamp = DateTime.UtcNow });

    public async Task NotifyDeviceVersionChanged(string deviceId, string newVersion)
        => await _deploymentHub.Clients.All
            .SendAsync("device.versionChanged", new { deviceId, newVersion, timestamp = DateTime.UtcNow });
}
