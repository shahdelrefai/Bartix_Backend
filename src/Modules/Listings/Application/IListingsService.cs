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

    Task<IReadOnlyList<ListingResponse>> GetByIdsAsync(IReadOnlyList<Guid> listingIds, CancellationToken cancellationToken);

    Task<ListingResponse> UpdateStatusAsync(Guid ownerUserId, Guid listingId, UpdateListingStatusRequest request, CancellationToken cancellationToken);

    // ─── Favourites ─────────────────────────────────────────────────────
    Task<FavouriteStateResponse> ToggleFavouriteAsync(Guid userId, Guid listingId, CancellationToken cancellationToken);

    Task AddFavouriteAsync(Guid userId, Guid listingId, CancellationToken cancellationToken);

    Task RemoveFavouriteAsync(Guid userId, Guid listingId, CancellationToken cancellationToken);

    Task<bool> IsFavouriteAsync(Guid userId, Guid listingId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ListingResponse>> GetFavouritesAsync(Guid userId, CancellationToken cancellationToken);

    // ─── Views & reports ────────────────────────────────────────────────
    Task IncrementViewAsync(Guid listingId, Guid userId, CancellationToken cancellationToken);

    Task ReportAsync(Guid listingId, Guid userId, CancellationToken cancellationToken);
}
