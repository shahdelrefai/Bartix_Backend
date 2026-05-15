namespace Bartrix.Modules.Listings.Contracts;

public sealed record UpdateListingRequest(
    string Title,
    string Description,
    string Category,
    string Condition,
    string Location,
    decimal? AskingPrice,
    bool IsAvailableForSwap,
    bool IsAvailableForSale,
    IReadOnlyList<string>? ImageUrls);
