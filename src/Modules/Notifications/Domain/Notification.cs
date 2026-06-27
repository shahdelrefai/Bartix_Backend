namespace Bartrix.Modules.Notifications.Domain;

/// <summary>In-app notification (mirrors the Firebase Notifications collection).</summary>
public sealed class Notification
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid UserId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    /// <summary>productUpdate | tradeUpdate | system | chatMessage (mirrors the Flutter NotificationType enum).</summary>
    public string Type { get; private set; } = "system";

    public string? RelatedId { get; private set; }

    public bool IsRead { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private Notification()
    {
    }

    public Notification(
        Guid userId,
        string title,
        string body,
        string type,
        string? relatedId,
        DateTimeOffset createdAtUtc)
    {
        UserId = userId;
        Title = title;
        Body = body;
        Type = string.IsNullOrWhiteSpace(type) ? "system" : type;
        RelatedId = relatedId;
        CreatedAtUtc = createdAtUtc;
    }

    public void MarkRead() => IsRead = true;
}

public static class NotificationTypes
{
    public const string ProductUpdate = "productUpdate";
    public const string TradeUpdate = "tradeUpdate";
    public const string System = "system";
    public const string ChatMessage = "chatMessage";
}
