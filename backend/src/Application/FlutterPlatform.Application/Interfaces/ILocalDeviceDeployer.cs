namespace FlutterPlatform.Application.Interfaces;

public interface ILocalDeviceDeployer
{
    Task<LocalDeployResult> DeployAsync(Guid projectId, CancellationToken ct = default);
}

public record LocalDeployResult(bool Success, string Log, string? Error);
