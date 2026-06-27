namespace Bartrix.Modules.Listings.Domain;

/// <summary>Records that a user reported a listing (mirrors Firebase reportedByUserIds).</summary>
public sealed class ListingReport
{
    public Guid ListingId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private ListingReport()
    {
    }

    public ListingReport(Guid listingId, Guid userId, DateTimeOffset createdAtUtc)
    {
        ListingId = listingId;
        UserId = userId;
        CreatedAtUtc = createdAtUtc;
    }
}
