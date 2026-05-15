namespace Bartrix.Modules.Search.Contracts;

public sealed record PagedSearchResponse(
    IReadOnlyList<SearchResultResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
