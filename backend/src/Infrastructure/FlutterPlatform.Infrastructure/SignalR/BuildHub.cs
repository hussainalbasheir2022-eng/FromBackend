using Microsoft.AspNetCore.SignalR;

namespace FlutterPlatform.Infrastructure.SignalR;

public class BuildHub : Hub
{
    public async Task JoinBuildGroup(string buildId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"build-{buildId}");

    public async Task LeaveBuildGroup(string buildId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"build-{buildId}");
}
