using Bartrix.BuildingBlocks.Authentication;
using Bartrix.Modules.Auth.Application;
using Bartrix.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Bartrix.Modules.Auth.Infrastructure;

public static class AuthModuleServiceCollectionExtensions
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services)
    {
        services.AddDbContext<AuthDbContext>((serviceProvider, options) =>
        {
            var dataSource = serviceProvider.GetRequiredService<NpgsqlDataSource>();
            options.UseNpgsql(dataSource);
        });

        services.AddScoped<IAuthService, AuthService>();
        services.AddOptions<GoogleAuthOptions>();
        services.AddHttpClient<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IRefreshTokenService, RefreshTokenService>();
        services.AddSingleton<IOtpProvider>(sp =>
            new LocalMockOtpProvider(
                sp.GetRequiredService<IOptions<OtpOptions>>().Value,
                sp.GetRequiredService<TimeProvider>()));
        services.AddSingleton<IEmailOtpService, InMemoryEmailOtpService>();
        services.AddSingleton<IDatabaseInitializer, AuthDatabaseInitializer>();
        services.AddHostedService<PremiumExpirationHostedService>();

        return services;
    }
}
