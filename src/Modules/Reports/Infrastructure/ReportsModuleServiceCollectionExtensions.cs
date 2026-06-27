using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Reports.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Bartrix.Modules.Reports.Infrastructure;

public static class ReportsModuleServiceCollectionExtensions
{
    public static IServiceCollection AddReportsModule(this IServiceCollection services)
    {
        services.AddDbContext<ReportsDbContext>((sp, options) =>
            options.UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>()));

        services.AddScoped<IReportsService, ReportsService>();
        services.AddSingleton<IAdminUserReader, NpgsqlAdminUserReader>();
        services.AddSingleton<IDatabaseInitializer, ReportsDatabaseInitializer>();

        return services;
    }
}
