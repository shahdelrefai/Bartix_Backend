namespace Bartrix.Modules.Listings.Contracts;

public sealed record CreateListingRequest(
    string Title,
    string Description,
    string Category,
    string Condition,
    string Location,
    decimal? AskingPrice,
    bool IsAvailableForSwap,
    bool IsAvailableForSale,
    IReadOnlyList<string>? ImageUrls);
