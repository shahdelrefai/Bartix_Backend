using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Payments.Application;
using Bartrix.Modules.Payments.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Bartrix.Modules.Payments.Infrastructure;

public static class PaymentsModuleServiceCollectionExtensions
{
    public static IServiceCollection AddPaymentsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PaymentsDbContext>((sp, options) =>
        {
            var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
            options.UseNpgsql(dataSource);
        });

        services.AddOptions<PaymobOptions>()
            .Bind(configuration.GetRequiredSection(PaymobOptions.SectionName));

        services.AddHttpClient<IPaymobClient, PaymobHttpClient>();

        services.AddScoped<IPaymentService, PaymentService>();
        services.AddSingleton<IDatabaseInitializer, PaymentsDatabaseInitializer>();

        return services;
    }
}
