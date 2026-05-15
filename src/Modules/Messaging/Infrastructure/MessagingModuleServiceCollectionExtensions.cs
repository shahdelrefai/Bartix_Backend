using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Messaging.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace Bartrix.Modules.Messaging.Infrastructure;

public static class MessagingModuleServiceCollectionExtensions
{
    public static IServiceCollection AddMessagingModule(this IServiceCollection services)
    {
        services.AddDbContext<MessagingDbContext>((serviceProvider, options) =>
        {
            var dataSource = serviceProvider.GetRequiredService<NpgsqlDataSource>();
            options.UseNpgsql(dataSource);
        });

        services.AddScoped<IMessagingService, MessagingService>();
        services.AddSingleton<ITradeMessagingReader, NpgsqlTradeMessagingReader>();
        services.TryAddSingleton<IConversationRealtimeNotifier, NoOpConversationRealtimeNotifier>();
        services.AddSingleton<IDatabaseInitializer, MessagingDatabaseInitializer>();

        return services;
    }
}
