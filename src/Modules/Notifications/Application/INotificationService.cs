using Bartrix.Modules.Notifications.Contracts;

namespace Bartrix.Modules.Notifications.Application;

public interface INotificationService
{
    Task<NotificationResponse> CreateAsync(Guid userId, string title, string body, string type, string? relatedId, CancellationToken cancellationToken);

    Task<IReadOnlyList<NotificationResponse>> GetForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken);

    Task MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken);

    Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken);
}
