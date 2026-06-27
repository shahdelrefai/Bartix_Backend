using Bartrix.Modules.Notifications.Contracts;

namespace Bartrix.Modules.Notifications.Application;

public interface INotificationRealtimeNotifier
{
    Task NotifyAsync(Guid userId, NotificationResponse notification, int unreadCount, CancellationToken cancellationToken);
}
