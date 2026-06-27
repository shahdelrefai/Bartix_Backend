namespace Bartrix.Modules.Search.Contracts;

public sealed class SearchQuery
{
    public string? Search { get; init; }
    public string? Category { get; init; }
    public string? Location { get; init; }
    public string? Type { get; init; }
    public Guid? OwnerUserId { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string? Condition { get; init; }
    public string? Sort { get; init; }  // newest (default), price_asc, price_desc
}
