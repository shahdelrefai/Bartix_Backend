namespace Bartrix.Modules.Services.Contracts;

public sealed record ServiceOfferResponse(
    Guid Id,
    Guid OwnerUserId,
    string Title,
    string Description,
    string Category,
    string Location,
    string FulfillmentMode,
    string PricingType,
    decimal? PriceAmount,
    bool IsAvailableForTrade,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
