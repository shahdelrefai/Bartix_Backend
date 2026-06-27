namespace Bartrix.Modules.Search.Application;

public sealed record SearchCatalogRequest(
    string? Search,
    string? Category,
    string? Location,
    SearchSourceType SourceType,
    Guid? OwnerUserId,
    int Page,
    int PageSize,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? Condition,
    string? Sort);
