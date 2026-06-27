using Bartrix.BuildingBlocks.Notifications;
using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Notifications.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace Bartrix.Modules.Notifications.Infrastructure;

public static class NotificationsModuleServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        services.AddDbContext<NotificationsDbContext>((serviceProvider, options) =>
        {
            var dataSource = serviceProvider.GetRequiredService<NpgsqlDataSource>();
            options.UseNpgsql(dataSource);
        });

        services.AddScoped<NotificationService>();
        services.AddScoped<INotificationService>(sp => sp.GetRequiredService<NotificationService>());
        services.AddScoped<INotificationPublisher>(sp => sp.GetRequiredService<NotificationService>());
        services.AddSingleton<IUserLanguageReader, NpgsqlUserLanguageReader>();
        services.TryAddSingleton<INotificationRealtimeNotifier, NoOpNotificationRealtimeNotifier>();
        services.AddSingleton<IDatabaseInitializer, NotificationsDatabaseInitializer>();

        return services;
    }
}
