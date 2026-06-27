using Bartrix.BuildingBlocks.Listings;
using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Listings.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Bartrix.Modules.Listings.Infrastructure;

public static class ListingsModuleServiceCollectionExtensions
{
    public static IServiceCollection AddListingsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ListingsDbContext>((serviceProvider, options) =>
        {
            var dataSource = serviceProvider.GetRequiredService<NpgsqlDataSource>();
            options.UseNpgsql(dataSource);
        });

        services.AddScoped<IListingsService, ListingsService>();
        services.AddSingleton<IListingStatusWriter, NpgsqlListingStatusWriter>();
        services.AddSingleton<IDatabaseInitializer, ListingsDatabaseInitializer>();

        var openAiKey = configuration["OpenAI:ApiKey"];
        if (!string.IsNullOrWhiteSpace(openAiKey))
        {
            services.AddHttpClient<IAiDescriptionService, OpenAiDescriptionService>(
                (sp, client) => new OpenAiDescriptionService(
                    client, openAiKey,
                    sp.GetRequiredService<ILogger<OpenAiDescriptionService>>()));
        }
        else
        {
            services.AddSingleton<IAiDescriptionService, FallbackAiDescriptionService>();
        }

        return services;
    }
}
