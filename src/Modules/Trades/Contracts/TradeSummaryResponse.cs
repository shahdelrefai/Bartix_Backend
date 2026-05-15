namespace Bartrix.Modules.Trades.Contracts;

public sealed record TradeSummaryResponse(
    Guid Id,
    Guid SenderUserId,
    Guid ReceiverUserId,
    Guid RequestedListingId,
    IReadOnlyList<Guid> OfferedListingIds,
    string? Message,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
