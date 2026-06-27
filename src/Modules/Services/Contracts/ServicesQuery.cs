namespace Bartrix.Modules.Services.Contracts;

public sealed class ServicesQuery
{
    public string? Search { get; init; }

    public string? Category { get; init; }

    public string? Location { get; init; }

    public string? FulfillmentMode { get; init; }

    public string? PricingType { get; init; }

    public Guid? OwnerUserId { get; init; }

    public bool? OnlyActive { get; init; }

    public int? Page { get; init; }

    public int? PageSize { get; init; }
}
