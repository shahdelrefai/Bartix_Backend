namespace Bartrix.Modules.Listings.Domain;

/// <summary>A user's favourite listing (mirrors Firebase favouriteProductIds / interestedUsers).</summary>
public sealed class ListingFavorite
{
    public Guid UserId { get; private set; }

    public Guid ListingId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private ListingFavorite()
    {
    }

    public ListingFavorite(Guid userId, Guid listingId, DateTimeOffset createdAtUtc)
    {
        UserId = userId;
        ListingId = listingId;
        CreatedAtUtc = createdAtUtc;
    }
}
