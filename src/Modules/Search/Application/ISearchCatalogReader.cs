namespace Bartrix.Modules.Search.Application;

public interface ISearchCatalogReader
{
    Task<SearchCatalogPage> SearchAsync(SearchCatalogRequest request, CancellationToken cancellationToken);
}
