using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Services.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Bartrix.Modules.Services.Infrastructure;

public static class ServicesModuleServiceCollectionExtensions
{
    public static IServiceCollection AddServicesModule(this IServiceCollection services)
    {
        services.AddDbContext<ServicesDbContext>((serviceProvider, options) =>
        {
            var dataSource = serviceProvider.GetRequiredService<NpgsqlDataSource>();
            options.UseNpgsql(dataSource);
        });

        services.AddScoped<IServicesService, ServicesService>();
        services.AddSingleton<IDatabaseInitializer, ServicesDatabaseInitializer>();

        return services;
    }
}
