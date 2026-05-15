using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Reputation.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Bartrix.Modules.Reputation.Infrastructure;

public static class ReputationModuleServiceCollectionExtensions
{
    public static IServiceCollection AddReputationModule(this IServiceCollection services)
    {
        services.AddDbContext<ReputationDbContext>((serviceProvider, options) =>
        {
            var dataSource = serviceProvider.GetRequiredService<NpgsqlDataSource>();
            options.UseNpgsql(dataSource);
        });

        services.AddScoped<IReputationService, ReputationService>();
        services.AddSingleton<ITradeReputationReader, NpgsqlTradeReputationReader>();
        services.AddSingleton<IDatabaseInitializer, ReputationDatabaseInitializer>();

        return services;
    }
}
