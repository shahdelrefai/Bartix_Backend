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
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
        var sourceType = ParseType(query.Type);

        var request = new SearchCatalogRequest(
            NormalizeOptional(query.Search, 200),
            NormalizeOptional(query.Category, 100),
            NormalizeOptional(query.Location, 200),
            sourceType,
            query.OwnerUserId,
            page,
            pageSize);

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
