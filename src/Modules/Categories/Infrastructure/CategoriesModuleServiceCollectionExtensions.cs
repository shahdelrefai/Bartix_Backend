using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Categories.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Bartrix.Modules.Categories.Infrastructure;

public static class CategoriesModuleServiceCollectionExtensions
{
    public static IServiceCollection AddCategoriesModule(this IServiceCollection services)
    {
        services.AddDbContext<CategoriesDbContext>((sp, options) =>
            options.UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>()));

        services.AddScoped<ICategoriesService, CategoriesService>();
        services.AddSingleton<IDatabaseInitializer, CategoriesDatabaseInitializer>();

        return services;
    }
}
