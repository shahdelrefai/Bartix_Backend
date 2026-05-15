namespace Bartrix.Modules.Services.Contracts;

public sealed record CreateServiceOfferRequest(
    string Title,
    string Description,
    string Category,
    string Location,
    string FulfillmentMode,
    string PricingType,
    decimal? PriceAmount,
    bool IsAvailableForTrade);
