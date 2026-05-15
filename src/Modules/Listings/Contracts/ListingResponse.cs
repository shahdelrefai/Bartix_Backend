namespace Bartrix.Modules.Listings.Contracts;

public sealed record ListingResponse(
    Guid Id,
    Guid OwnerUserId,
    string Title,
    string Description,
    string Category,
    string Condition,
    string Location,
    decimal? AskingPrice,
    bool IsAvailableForSwap,
    bool IsAvailableForSale,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<ListingImageResponse> Images);
