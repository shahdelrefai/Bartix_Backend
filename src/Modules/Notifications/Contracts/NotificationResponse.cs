namespace Bartrix.Modules.Notifications.Contracts;

public sealed record NotificationResponse(
    Guid Id,
    Guid UserId,
    string Title,
    string Body,
    string Type,
    string? RelatedId,
    bool IsRead,
    DateTimeOffset CreatedAtUtc);

public sealed record UnreadCountResponse(int UnreadCount);
