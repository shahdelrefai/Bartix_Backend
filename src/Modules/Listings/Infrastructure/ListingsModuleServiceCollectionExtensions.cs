using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Listings.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Bartrix.Modules.Listings.Infrastructure;

public static class ListingsModuleServiceCollectionExtensions
{
    public static IServiceCollection AddListingsModule(this IServiceCollection services)
    {
        services.AddDbContext<ListingsDbContext>((serviceProvider, options) =>
        {
            var dataSource = serviceProvider.GetRequiredService<NpgsqlDataSource>();
            options.UseNpgsql(dataSource);
        });

        services.AddScoped<IListingsService, ListingsService>();
        services.AddSingleton<IDatabaseInitializer, ListingsDatabaseInitializer>();

        return services;
    }
}
