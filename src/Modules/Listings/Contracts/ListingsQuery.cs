namespace Bartrix.Modules.Listings.Contracts;

public sealed class ListingsQuery
{
    public string? Search { get; init; }

    public string? Category { get; init; }

    public string? Condition { get; init; }

    public string? Location { get; init; }

    public Guid? OwnerUserId { get; init; }

    public bool? OnlyActive { get; init; }

    public int? Page { get; init; }

    public int? PageSize { get; init; }
}
