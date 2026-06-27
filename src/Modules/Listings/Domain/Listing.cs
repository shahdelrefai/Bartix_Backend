namespace Bartrix.Modules.Listings.Domain;

public sealed class Listing
{
    private readonly List<ListingImage> _images = new();

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid OwnerUserId { get; private set; }

    public string? OwnerName { get; private set; }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public string Category { get; private set; }

    /// <summary>Free-form condition string (e.g. new_item, like_new, good, fair, poor).</summary>
    public string Condition { get; private set; }

    public string Location { get; private set; }

    public decimal? AskingPrice { get; private set; }

    public bool IsAvailableForSwap { get; private set; }

    public bool IsAvailableForSale { get; private set; }

    public bool IsActive { get; private set; }

    // ─── Product-parity fields (mirror the Firebase Products document) ────
    public string Type { get; private set; } = "item";          // item | service

    public string Status { get; private set; } = "available";    // available | traded | reserved | unavailable

    public string TransactionType { get; private set; } = "barter"; // sell | barter

    public decimal? Price { get; private set; }

    public string? DesiredSwapCategory { get; private set; }

    public string? CustomCategory { get; private set; }

    public double? Latitude { get; private set; }

    public double? Longitude { get; private set; }

    public List<string> Tags { get; private set; } = new();

    public int ViewCount { get; private set; }

    public bool IsOwnerPremium { get; private set; }

    // Service-specific fields
    public string? ServiceCategory { get; private set; }

    public string? CustomServiceCategory { get; private set; }

    public int? EstimatedDuration { get; private set; }

    public decimal? PriceRange { get; private set; }

    public string? AvailabilitySchedule { get; private set; }

    public List<string> Skills { get; private set; } = new();

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<ListingImage> Images => _images;

    private Listing()
    {
        Title = string.Empty;
        Description = string.Empty;
        Category = string.Empty;
        Condition = string.Empty;
        Location = string.Empty;
    }

    public Listing(
        Guid ownerUserId,
        string title,
        string description,
        string category,
        string condition,
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
        string condition,
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

    /// <summary>Applies the product-parity fields. Safe to call with defaults from the simpler create path.</summary>
    public void SetProductDetails(
        string? ownerName,
        string type,
        string transactionType,
        decimal? price,
        string? desiredSwapCategory,
        string? customCategory,
        double? latitude,
        double? longitude,
        IEnumerable<string>? tags,
        bool isOwnerPremium,
        string? serviceCategory,
        string? customServiceCategory,
        int? estimatedDuration,
        decimal? priceRange,
        string? availabilitySchedule,
        IEnumerable<string>? skills)
    {
        OwnerName = ownerName;
        Type = string.IsNullOrWhiteSpace(type) ? "item" : type;
        TransactionType = string.IsNullOrWhiteSpace(transactionType) ? "barter" : transactionType;
        Price = price;
        DesiredSwapCategory = desiredSwapCategory;
        CustomCategory = customCategory;
        Latitude = latitude;
        Longitude = longitude;
        Tags = tags?.ToList() ?? new List<string>();
        IsOwnerPremium = isOwnerPremium;
        ServiceCategory = serviceCategory;
        CustomServiceCategory = customServiceCategory;
        EstimatedDuration = estimatedDuration;
        PriceRange = priceRange;
        AvailabilitySchedule = availabilitySchedule;
        Skills = skills?.ToList() ?? new List<string>();
    }

    public void SetStatus(string status, DateTimeOffset updatedAtUtc)
    {
        Status = status;
        // Keep the legacy availability flag in sync with the status.
        UpdatedAtUtc = updatedAtUtc;
    }

    public void IncrementViewCount()
    {
        ViewCount += 1;
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
