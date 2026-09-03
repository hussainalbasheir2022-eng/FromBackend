namespace FlutterPlatform.Application.Interfaces;

public interface ISignalRNotifier
{
    Task NotifyBuildStarted(Guid buildId, string projectName);
    Task NotifyBuildLog(Guid buildId, string message, string level = "info");
    Task NotifyBuildCompleted(Guid buildId, bool success, string? artifactUrl = null);
    Task NotifyBuildFailed(Guid buildId, string error);
    Task NotifyReleasePublished(Guid releaseId, string applicationId, string version, bool mandatory);
    Task NotifyDeploymentStarted(Guid deploymentId, Guid releaseId);
    Task NotifyDeploymentProgress(Guid deploymentId, string deviceId, string status, int? progressPercent);
    Task NotifyDeploymentCompleted(Guid deploymentId);
    Task NotifyDeviceOnline(string deviceId, string appVersion);
    Task NotifyDeviceOffline(string deviceId);
    Task NotifyDeviceVersionChanged(string deviceId, string newVersion);
}
