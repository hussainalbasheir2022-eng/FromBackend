using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FlutterPlatform.Infrastructure.SignalR;

[AllowAnonymous]
public class DeploymentHub : Hub
{
    // Device clients connect here using their deviceId as group
    public async Task RegisterDevice(string deviceId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"device-{deviceId}");

    public async Task JoinApplicationGroup(string applicationId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"app-{applicationId}");

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
