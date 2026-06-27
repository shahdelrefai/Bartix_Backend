using Bartrix.BuildingBlocks.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Bartrix.Modules.Auth.Infrastructure;

public sealed class PremiumExpirationHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PremiumExpirationHostedService> _logger;

    public PremiumExpirationHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<PremiumExpirationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await ExpireAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to expire premium subscriptions.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ExpireAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IPostgresConnectionFactory>();
        await using var conn = await factory.OpenConnectionAsync(ct);

        await using var cmd = new NpgsqlCommand("""
            WITH expired AS (
                UPDATE auth.user_accounts
                   SET is_premium_active = false
                 WHERE is_premium_active = true
                   AND premium_expires_at_utc < NOW()
                RETURNING id
            )
            UPDATE listings.listings
               SET is_owner_premium = false
             WHERE owner_user_id IN (SELECT id FROM expired)
            """, conn);

        var rows = await cmd.ExecuteNonQueryAsync(ct);
        if (rows > 0)
            _logger.LogInformation("Expired premium for {Count} user(s) / listing(s).", rows);
    }
}
