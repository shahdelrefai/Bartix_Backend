namespace Bartrix.Modules.Trades.Contracts;

public sealed record TradeCounterOfferResponse(
    Guid Id,
    Guid TradeProposalId,
    Guid FromUserId,
    Guid ToUserId,
    IReadOnlyList<Guid> OfferedListingIds,
    IReadOnlyList<Guid> RequestedListingIds,
    string? Message,
    bool IsAccepted,
    DateTimeOffset CreatedAtUtc);
