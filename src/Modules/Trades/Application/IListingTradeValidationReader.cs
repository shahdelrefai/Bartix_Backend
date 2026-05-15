namespace Bartrix.Modules.Trades.Application;

public interface IListingTradeValidationReader
{
    Task<ListingOwnershipSnapshot?> GetListingAsync(Guid listingId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ListingOwnershipSnapshot>> GetListingsAsync(IEnumerable<Guid> listingIds, CancellationToken cancellationToken);
}
