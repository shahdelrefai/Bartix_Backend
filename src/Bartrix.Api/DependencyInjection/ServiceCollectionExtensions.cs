using System.Text;
using Bartrix.BuildingBlocks.Authentication;
using Bartrix.BuildingBlocks.Persistence;
using Bartrix.BuildingBlocks.Realtime;
using Bartrix.BuildingBlocks.Storage;
using Bartrix.Modules.Auth.Infrastructure;
using Bartrix.Modules.Delivery.Infrastructure;
using Bartrix.Modules.Listings.Infrastructure;
using Bartrix.Modules.Messaging.Application;
using Bartrix.Modules.Messaging.Infrastructure;
using Bartrix.Modules.Profiles.Infrastructure;
using Bartrix.Modules.Reputation.Infrastructure;
using Bartrix.Modules.Search.Infrastructure;
using Bartrix.Modules.Services.Infrastructure;
using Bartrix.Modules.Trades.Infrastructure;
using Bartrix.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Bartrix.Api.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBartrixPlatform(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddProblemDetails();
        services.AddEndpointsApiExplorer();
        services.AddHealthChecks();
        services.AddAuthorization();
        services.AddSingleton(TimeProvider.System);

        services.AddConfiguredOptions<JwtOptions>(configuration, JwtOptions.SectionName);
        services.AddConfiguredOptions<RefreshTokenOptions>(configuration, RefreshTokenOptions.SectionName);
        services.AddConfiguredOptions<OtpOptions>(configuration, OtpOptions.SectionName);
        services.AddConfiguredOptions<MinioOptions>(configuration, MinioOptions.SectionName);
        services.AddConfiguredOptions<SignalROptions>(configuration, SignalROptions.SectionName);

        var databaseConnectionString = configuration.GetConnectionString("Database");
        EnsureValue(databaseConnectionString, "ConnectionStrings:Database");

        var jwtOptions = configuration.GetRequiredSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException($"Missing configuration section '{JwtOptions.SectionName}'.");

        ValidateJwtOptions(jwtOptions);

        var signalROptions = configuration.GetRequiredSection(SignalROptions.SectionName).Get<SignalROptions>()
            ?? new SignalROptions();

        services.AddSingleton(sp =>
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(databaseConnectionString);
            return dataSourceBuilder.Build();
        });

        services.AddSingleton<IPostgresConnectionFactory, NpgsqlPostgresConnectionFactory>();
        services.AddSingleton<IMinioClientFactory>(sp =>
            new MinioClientFactory(sp.GetRequiredService<IOptions<MinioOptions>>().Value));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    ClockSkew = TimeSpan.FromSeconds(jwtOptions.ClockSkewSeconds)
                };
            });

        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = signalROptions.EnableDetailedErrors;
        });

        services.AddAuthModule();
        services.AddProfilesModule();
        services.AddListingsModule();
        services.AddTradesModule();
        services.AddMessagingModule();
        services.AddReputationModule();
        services.AddDeliveryModule();
        services.AddServicesModule();
        services.AddSearchModule();
        services.Replace(ServiceDescriptor.Singleton<IConversationRealtimeNotifier>(sp =>
            new SignalRConversationRealtimeNotifier(sp.GetRequiredService<IHubContext<TradeChatHub>>())));

        return services;
    }

    private static IServiceCollection AddConfiguredOptions<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName) where TOptions : class
    {
        services.AddOptions<TOptions>()
            .Bind(configuration.GetRequiredSection(sectionName));

        return services;
    }

    private static void ValidateJwtOptions(JwtOptions options)
    {
        EnsureValue(options.Issuer, $"{JwtOptions.SectionName}:Issuer");
        EnsureValue(options.Audience, $"{JwtOptions.SectionName}:Audience");
        EnsureValue(options.SigningKey, $"{JwtOptions.SectionName}:SigningKey");

        if (options.AccessTokenMinutes <= 0)
        {
            throw new InvalidOperationException($"{JwtOptions.SectionName}:AccessTokenMinutes must be greater than 0.");
        }

        if (options.ClockSkewSeconds < 0)
        {
            throw new InvalidOperationException($"{JwtOptions.SectionName}:ClockSkewSeconds cannot be negative.");
        }
    }

    private static void EnsureValue(string? value, string settingName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing required configuration value '{settingName}'.");
        }
    }
}
