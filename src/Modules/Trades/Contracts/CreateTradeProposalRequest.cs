namespace Bartrix.Modules.Trades.Contracts;

public sealed record CreateTradeProposalRequest(
    Guid RequestedListingId,
    IReadOnlyList<Guid> OfferedListingIds,
    string? Message);
