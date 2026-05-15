using Bartrix.Modules.Listings.Contracts;

namespace Bartrix.Modules.Listings.Application;

public interface IListingsService
{
    Task<ListingResponse> CreateAsync(Guid ownerUserId, CreateListingRequest request, CancellationToken cancellationToken);

    Task<ListingResponse> UpdateAsync(Guid ownerUserId, Guid listingId, UpdateListingRequest request, CancellationToken cancellationToken);

    Task ArchiveAsync(Guid ownerUserId, Guid listingId, CancellationToken cancellationToken);

    Task<ListingResponse?> GetByIdAsync(Guid listingId, CancellationToken cancellationToken);

    Task<PagedListingsResponse> BrowseAsync(ListingsQuery query, CancellationToken cancellationToken);

    Task<IReadOnlyList<ListingResponse>> GetMineAsync(Guid ownerUserId, CancellationToken cancellationToken);
}
