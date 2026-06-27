using Bartrix.BuildingBlocks.Notifications;
using Bartrix.Modules.Notifications.Contracts;
using Bartrix.Modules.Notifications.Domain;
using Bartrix.Modules.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Notifications.Application;

public sealed class NotificationService : INotificationService, INotificationPublisher
{
    private readonly NotificationsDbContext _dbContext;
    private readonly INotificationRealtimeNotifier _notifier;
    private readonly IUserLanguageReader _languageReader;
    private readonly TimeProvider _timeProvider;

    public NotificationService(
        NotificationsDbContext dbContext,
        INotificationRealtimeNotifier notifier,
        IUserLanguageReader languageReader,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _notifier = notifier;
        _languageReader = languageReader;
        _timeProvider = timeProvider;
    }

    public async Task<NotificationResponse> CreateAsync(Guid userId, string title, string body, string type, string? relatedId, CancellationToken cancellationToken)
    {
        var notification = new Notification(userId, title, body, type, relatedId, _timeProvider.GetUtcNow());
        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = Map(notification);
        var unread = await GetUnreadCountAsync(userId, cancellationToken);
        await _notifier.NotifyAsync(userId, response, unread, cancellationToken);
        return response;
    }

    public async Task PublishAsync(NotificationPublishRequest request, CancellationToken cancellationToken)
    {
        var language = await _languageReader.GetLanguageCodeAsync(request.UserId, cancellationToken);
        var title = NotificationTemplates.Render(language, request.TitleKey, request.BodyArgs);
        var body = NotificationTemplates.Render(language, request.BodyKey, request.BodyArgs);
        await CreateAsync(request.UserId, title, body, request.Type, request.RelatedId, cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationResponse>> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var items = await _dbContext.Notifications
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        return items.Select(Map).ToList();
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.Notifications.CountAsync(x => x.UserId == userId && !x.IsRead, cancellationToken);
    }

    public async Task MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken)
    {
        var notification = await _dbContext.Notifications
            .SingleOrDefaultAsync(x => x.Id == notificationId, cancellationToken);

        if (notification is null || notification.UserId != userId)
        {
            throw new NotificationValidationException("Notification was not found.");
        }

        if (!notification.IsRead)
        {
            notification.MarkRead();
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken)
    {
        var unread = await _dbContext.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ToListAsync(cancellationToken);

        if (unread.Count == 0)
        {
            return;
        }

        foreach (var notification in unread)
        {
            notification.MarkRead();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static NotificationResponse Map(Notification notification)
    {
        return new NotificationResponse(
            notification.Id,
            notification.UserId,
            notification.Title,
            notification.Body,
            notification.Type,
            notification.RelatedId,
            notification.IsRead,
            notification.CreatedAtUtc);
    }
}
