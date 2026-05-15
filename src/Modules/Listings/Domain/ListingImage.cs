namespace Bartrix.Modules.Listings.Domain;

public sealed class ListingImage
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid ListingId { get; private set; }

    public string Url { get; private set; }

    public int SortOrder { get; private set; }

    public Listing Listing { get; private set; } = null!;

    private ListingImage()
    {
        Url = string.Empty;
    }

    public ListingImage(Guid listingId, string url, int sortOrder)
    {
        ListingId = listingId;
        Url = url;
        SortOrder = sortOrder;
    }
}
