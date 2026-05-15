namespace Bartrix.Modules.Trades.Application;

public sealed record ListingOwnershipSnapshot(
    Guid ListingId,
    Guid OwnerUserId,
    bool IsActive);
