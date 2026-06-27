namespace Bartrix.Modules.Trades.Contracts;

public sealed record AddCounterOfferRequest(
    IReadOnlyList<Guid> OfferedListingIds,
    IReadOnlyList<Guid> RequestedListingIds,
    string? Message);
