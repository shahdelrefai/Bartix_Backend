using Bartrix.Modules.Search.Contracts;

namespace Bartrix.Modules.Search.Application;

public interface ISearchService
{
    Task<PagedSearchResponse> SearchAsync(SearchQuery query, CancellationToken cancellationToken);
}
