namespace Bartrix.Modules.Delivery.Contracts;

public sealed record UpdateDeliveryRequest(
    string Method,
    string? Location,
    DateTimeOffset? ScheduledAtUtc,
    string? Notes);
