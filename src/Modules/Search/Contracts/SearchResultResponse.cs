namespace Bartrix.Modules.Search.Contracts;

public sealed record SearchResultResponse(
    Guid Id,
    string Type,
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
