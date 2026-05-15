namespace Bartrix.Modules.Services.Domain;

public sealed class ServiceOffer
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid OwnerUserId { get; private set; }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public string Category { get; private set; }

    public string Location { get; private set; }

    public ServiceFulfillmentMode FulfillmentMode { get; private set; }

    public ServicePricingType PricingType { get; private set; }

    public decimal? PriceAmount { get; private set; }

    public bool IsAvailableForTrade { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private ServiceOffer()
    {
        Title = string.Empty;
        Description = string.Empty;
        Category = string.Empty;
        Location = string.Empty;
    }

    public ServiceOffer(
        Guid ownerUserId,
        string title,
        string description,
        string category,
        string location,
        ServiceFulfillmentMode fulfillmentMode,
        ServicePricingType pricingType,
        decimal? priceAmount,
        bool isAvailableForTrade,
        DateTimeOffset createdAtUtc)
    {
        OwnerUserId = ownerUserId;
        Title = title;
        Description = description;
        Category = category;
        Location = location;
        FulfillmentMode = fulfillmentMode;
        PricingType = pricingType;
        PriceAmount = priceAmount;
        IsAvailableForTrade = isAvailableForTrade;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public void Update(
        string title,
        string description,
        string category,
        string location,
        ServiceFulfillmentMode fulfillmentMode,
        ServicePricingType pricingType,
        decimal? priceAmount,
        bool isAvailableForTrade,
        DateTimeOffset updatedAtUtc)
    {
        Title = title;
        Description = description;
        Category = category;
        Location = location;
        FulfillmentMode = fulfillmentMode;
        PricingType = pricingType;
        PriceAmount = priceAmount;
        IsAvailableForTrade = isAvailableForTrade;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Archive(DateTimeOffset updatedAtUtc)
    {
        IsActive = false;
        UpdatedAtUtc = updatedAtUtc;
    }
}
