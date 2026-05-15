using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Profiles.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Bartrix.Modules.Profiles.Infrastructure;

public static class ProfilesModuleServiceCollectionExtensions
{
    public static IServiceCollection AddProfilesModule(this IServiceCollection services)
    {
        services.AddDbContext<ProfilesDbContext>((serviceProvider, options) =>
        {
            var dataSource = serviceProvider.GetRequiredService<NpgsqlDataSource>();
            options.UseNpgsql(dataSource);
        });

        services.AddScoped<IProfileService, ProfileService>();
        services.AddSingleton<IDatabaseInitializer, ProfilesDatabaseInitializer>();

        return services;
    }
}
