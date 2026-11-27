using Microsoft.AspNetCore.SignalR;

namespace RpgTabletop.Api;

public static class RealtimeEvents
{
    public static Task PublishTableChangedAsync(IHubContext<SessionHub> hub) =>
        hub.Clients.All.SendAsync("TableChanged");
}
