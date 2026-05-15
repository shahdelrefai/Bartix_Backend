namespace Bartrix.Modules.Listings.Contracts;

public sealed record PagedListingsResponse(
    IReadOnlyList<ListingResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
