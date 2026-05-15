namespace Bartrix.Modules.Search.Application;

public sealed record SearchCatalogPage(
    IReadOnlyList<SearchCatalogItem> Items,
    int TotalCount);
