namespace Bartrix.Modules.Listings.Domain;

public sealed class Listing
{
    private readonly List<ListingImage> _images = new();

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid OwnerUserId { get; private set; }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public string Category { get; private set; }

    public ListingCondition Condition { get; private set; }

    public string Location { get; private set; }

    public decimal? AskingPrice { get; private set; }

    public bool IsAvailableForSwap { get; private set; }

    public bool IsAvailableForSale { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<ListingImage> Images => _images;

    private Listing()
    {
        Title = string.Empty;
        Description = string.Empty;
        Category = string.Empty;
        Location = string.Empty;
    }

    public Listing(
        Guid ownerUserId,
        string title,
        string description,
        string category,
        ListingCondition condition,
        string location,
        decimal? askingPrice,
        bool isAvailableForSwap,
        bool isAvailableForSale,
        DateTimeOffset createdAtUtc)
    {
        OwnerUserId = ownerUserId;
        Title = title;
        Description = description;
        Category = category;
        Condition = condition;
        Location = location;
        AskingPrice = askingPrice;
        IsAvailableForSwap = isAvailableForSwap;
        IsAvailableForSale = isAvailableForSale;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public void Update(
        string title,
        string description,
        string category,
        ListingCondition condition,
        string location,
        decimal? askingPrice,
        bool isAvailableForSwap,
        bool isAvailableForSale,
        DateTimeOffset updatedAtUtc)
    {
        Title = title;
        Description = description;
        Category = category;
        Condition = condition;
        Location = location;
        AskingPrice = askingPrice;
        IsAvailableForSwap = isAvailableForSwap;
        IsAvailableForSale = isAvailableForSale;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void ReplaceImages(IEnumerable<string> imageUrls)
    {
        _images.Clear();

        foreach (var image in imageUrls.Select((url, index) => new ListingImage(Id, url, index)))
        {
            _images.Add(image);
        }
    }

    public void Archive(DateTimeOffset updatedAtUtc)
    {
        IsActive = false;
        UpdatedAtUtc = updatedAtUtc;
    }
}
