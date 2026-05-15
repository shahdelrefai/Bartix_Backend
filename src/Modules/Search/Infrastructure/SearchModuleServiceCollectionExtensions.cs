using Bartrix.Modules.Search.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Bartrix.Modules.Search.Infrastructure;

public static class SearchModuleServiceCollectionExtensions
{
    public static IServiceCollection AddSearchModule(this IServiceCollection services)
    {
        services.AddScoped<ISearchService, SearchService>();
        services.AddSingleton<ISearchCatalogReader, NpgsqlSearchCatalogReader>();

        return services;
    }
}
