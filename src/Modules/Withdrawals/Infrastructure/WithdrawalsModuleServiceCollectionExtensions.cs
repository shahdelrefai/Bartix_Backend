using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Withdrawals.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Bartrix.Modules.Withdrawals.Infrastructure;

public static class WithdrawalsModuleServiceCollectionExtensions
{
    public static IServiceCollection AddWithdrawalsModule(this IServiceCollection services)
    {
        services.AddDbContext<WithdrawalsDbContext>((sp, options) =>
        {
            var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
            options.UseNpgsql(dataSource);
        });

        services.AddScoped<IWithdrawalService, WithdrawalService>();
        services.AddSingleton<IDatabaseInitializer, WithdrawalsDatabaseInitializer>();

        return services;
    }
}
