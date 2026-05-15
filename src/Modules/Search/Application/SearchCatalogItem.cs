namespace Bartrix.Modules.Search.Application;

public sealed record SearchCatalogItem(
    Guid Id,
    SearchSourceType Type,
    Guid OwnerUserId,
    string Title,
    string Description,
    string Category,
    string Location,
    decimal? PriceAmount,
    bool IsAvailableForTrade,
    string? ListingCondition,
    bool? IsAvailableForSale,
    string? FulfillmentMode,
    string? PricingType,
    DateTimeOffset CreatedAtUtc);
