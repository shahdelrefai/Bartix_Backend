namespace Bartrix.Modules.Delivery.Contracts;

public sealed record DeliveryResponse(
    Guid Id,
    Guid TradeProposalId,
    Guid ParticipantAUserId,
    Guid ParticipantBUserId,
    string? Method,
    string Status,
    string? Location,
    DateTimeOffset? ScheduledAtUtc,
    string? Notes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
