namespace Bartrix.BuildingBlocks.Listings;

/// <summary>
/// Cross-cutting port used by the Trades module to update listing status
/// (reserved on accept, traded on complete) without a module-to-module dependency.
/// </summary>
public interface IListingStatusWriter
{
    Task SetStatusAsync(Guid listingId, string status, CancellationToken cancellationToken);
    Task SetManyStatusAsync(IReadOnlyList<Guid> listingIds, string status, CancellationToken cancellationToken);
}
