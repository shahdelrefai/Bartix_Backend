using Bartrix.BuildingBlocks.Persistence;
using Bartrix.BuildingBlocks.Wallet;
using Bartrix.Modules.Wallet.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace Bartrix.Modules.Wallet.Infrastructure;

public static class WalletModuleServiceCollectionExtensions
{
    public static IServiceCollection AddWalletModule(this IServiceCollection services)
    {
        services.AddDbContext<WalletDbContext>((sp, options) =>
        {
            var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
            options.UseNpgsql(dataSource);
        });

        services.AddScoped<WalletService>();
        services.AddScoped<IWalletService>(sp => sp.GetRequiredService<WalletService>());
        services.TryAddSingleton<IWalletRealtimeNotifier, NoOpWalletRealtimeNotifier>();
        services.AddSingleton<IDatabaseInitializer, WalletsDatabaseInitializer>();

        return services;
    }
}
