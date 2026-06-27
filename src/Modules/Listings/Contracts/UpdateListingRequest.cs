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
    IReadOnlyList<string>? ImageUrls,
    // ─── Product-parity fields (all optional) ───────────────────────────
    string? OwnerName = null,
    string Type = "item",
    string TransactionType = "barter",
    decimal? Price = null,
    string? DesiredSwapCategory = null,
    string? CustomCategory = null,
    double? Latitude = null,
    double? Longitude = null,
    IReadOnlyList<string>? Tags = null,
    bool IsOwnerPremium = false,
    string? ServiceCategory = null,
    string? CustomServiceCategory = null,
    int? EstimatedDuration = null,
    decimal? PriceRange = null,
    string? AvailabilitySchedule = null,
    IReadOnlyList<string>? Skills = null);
