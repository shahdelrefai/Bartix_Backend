namespace Bartrix.Modules.Listings.Domain;

/// <summary>Records that a user viewed a listing (mirrors Firebase viewedUserIds).</summary>
public sealed class ListingView
{
    public Guid ListingId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private ListingView()
    {
    }

    public ListingView(Guid listingId, Guid userId, DateTimeOffset createdAtUtc)
    {
        ListingId = listingId;
        UserId = userId;
        CreatedAtUtc = createdAtUtc;
    }
}
