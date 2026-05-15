using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Delivery.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Bartrix.Modules.Delivery.Infrastructure;

public static class DeliveryModuleServiceCollectionExtensions
{
    public static IServiceCollection AddDeliveryModule(this IServiceCollection services)
    {
        services.AddDbContext<DeliveryDbContext>((serviceProvider, options) =>
        {
            var dataSource = serviceProvider.GetRequiredService<NpgsqlDataSource>();
            options.UseNpgsql(dataSource);
        });

        services.AddScoped<IDeliveryService, DeliveryService>();
        services.AddSingleton<ITradeDeliveryReader, NpgsqlTradeDeliveryReader>();
        services.AddSingleton<IDatabaseInitializer, DeliveryDatabaseInitializer>();

        return services;
    }
}
