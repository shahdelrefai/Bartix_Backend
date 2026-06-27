using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Trades.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Bartrix.Modules.Trades.Infrastructure;

public static class TradesModuleServiceCollectionExtensions
{
    public static IServiceCollection AddTradesModule(this IServiceCollection services)
    {
        services.AddDbContext<TradesDbContext>((serviceProvider, options) =>
        {
            var dataSource = serviceProvider.GetRequiredService<NpgsqlDataSource>();
            options.UseNpgsql(dataSource);
        });

        services.AddScoped<ITradesService, TradesService>();
        services.AddSingleton<IListingTradeValidationReader, NpgsqlListingTradeValidationReader>();
        services.AddSingleton<IDatabaseInitializer, TradesDatabaseInitializer>();
        services.AddHostedService<TradeExpirationHostedService>();

        return services;
    }
}
