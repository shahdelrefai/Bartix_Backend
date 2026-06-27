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
    DateTimeOffset UpdatedAtUtc,
    // ─── Parity fields ──────────────────────────────────────────────────
    IReadOnlyList<Guid>? RequestedListingIds = null,
    string Type = "any",
    string? SenderUserName = null,
    string? ReceiverUserName = null,
    string? RejectionReason = null,
    bool IsCounterOffer = false,
    Guid? ParentTradeId = null,
    bool IsFromPremium = false,
    IReadOnlyList<Guid>? DeliveryProvidedBy = null,
    DateTimeOffset? ExpiresAtUtc = null,
    IReadOnlyList<TradeCounterOfferResponse>? CounterOffers = null);
