namespace Bartrix.BuildingBlocks.Notifications;

/// <summary>
/// Cross-cutting port used by feature modules (Trades, Messaging, Reputation, …) to
/// emit an in-app notification to a user. Implemented by the Notifications module,
/// which resolves the recipient's language, renders the localized template, persists
/// the notification, and pushes it over SignalR. Lives in BuildingBlocks so emitting
/// modules don't take a dependency on the Notifications module.
/// </summary>
public interface INotificationPublisher
{
    Task PublishAsync(NotificationPublishRequest request, CancellationToken cancellationToken);
}

public sealed record NotificationPublishRequest(
    Guid UserId,
    string TitleKey,
    string BodyKey,
    string Type,
    IReadOnlyDictionary<string, string>? BodyArgs = null,
    string? RelatedId = null);
