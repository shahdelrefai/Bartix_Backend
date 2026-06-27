using Bartrix.Modules.Notifications.Application;
using Bartrix.Modules.Notifications.Contracts;

namespace Bartrix.Modules.Notifications.Infrastructure;

public sealed class NoOpNotificationRealtimeNotifier : INotificationRealtimeNotifier
{
    public Task NotifyAsync(Guid userId, NotificationResponse notification, int unreadCount, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
