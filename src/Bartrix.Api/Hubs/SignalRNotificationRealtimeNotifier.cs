using Bartrix.Modules.Notifications.Application;
using Bartrix.Modules.Notifications.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace Bartrix.Api.Hubs;

/// <summary>Pushes new in-app notifications and unread counts to a user's SignalR group.</summary>
public sealed class SignalRNotificationRealtimeNotifier : INotificationRealtimeNotifier
{
    private readonly IHubContext<TradeChatHub> _hubContext;

    public SignalRNotificationRealtimeNotifier(IHubContext<TradeChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyAsync(Guid userId, NotificationResponse notification, int unreadCount, CancellationToken cancellationToken)
    {
        var group = _hubContext.Clients.Group(TradeChatHub.ToUserGroup(userId.ToString()));
        await group.SendAsync("notificationReceived", notification, cancellationToken);
        await group.SendAsync("unreadCountChanged", unreadCount, cancellationToken);
    }
}
