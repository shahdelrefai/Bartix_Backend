using Bartrix.Modules.Search.Contracts;

namespace Bartrix.Modules.Search.Application;

public sealed class SearchService : ISearchService
{
    private readonly ISearchCatalogReader _catalogReader;

    public SearchService(ISearchCatalogReader catalogReader)
    {
        _catalogReader = catalogReader;
    }

    public async Task<PagedSearchResponse> SearchAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page.GetValueOrDefault(1) <= 0 ? 1 : query.Page.GetValueOrDefault(1);
        var requestedPageSize = query.PageSize.GetValueOrDefault(20);
        var pageSize = requestedPageSize <= 0 ? 20 : Math.Min(requestedPageSize, 100);
        var sourceType = ParseType(query.Type);

        var sort = query.Sort?.Trim().ToLowerInvariant() switch
        {
            "price_asc" => "price_asc",
            "price_desc" => "price_desc",
            _ => "newest"
        };

        var request = new SearchCatalogRequest(
            NormalizeOptional(query.Search, 200),
            NormalizeOptional(query.Category, 100),
            NormalizeOptional(query.Location, 200),
            sourceType,
            query.OwnerUserId,
            page,
            pageSize,
            query.MinPrice,
            query.MaxPrice,
            NormalizeOptional(query.Condition, 50),
            sort);

        var result = await _catalogReader.SearchAsync(request, cancellationToken);

        return new PagedSearchResponse(
            result.Items.Select(Map).ToList(),
            page,
            pageSize,
            result.TotalCount);
    }

    private static SearchSourceType ParseType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return SearchSourceType.All;
        }

        if (!Enum.TryParse<SearchSourceType>(type.Trim(), true, out var sourceType))
        {
            throw new SearchValidationException("Type must be 'All', 'Listings', or 'Services'.");
        }

        return sourceType;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new SearchValidationException($"Search field cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    private static SearchResultResponse Map(SearchCatalogItem item)
    {
        return new SearchResultResponse(
            item.Id,
            item.Type.ToString(),
            item.OwnerUserId,
            item.Title,
            item.Description,
            item.Category,
            item.Location,
            item.PriceAmount,
            item.IsAvailableForTrade,
            item.ListingCondition,
            item.IsAvailableForSale,
            item.FulfillmentMode,
            item.PricingType,
            item.CreatedAtUtc);
    }
}
