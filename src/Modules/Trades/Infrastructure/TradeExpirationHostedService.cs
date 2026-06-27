using Bartrix.Modules.Trades.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bartrix.Modules.Trades.Infrastructure;

/// <summary>
/// Periodically expires overdue pending trades (replaces the client-side
/// <c>checkAndExpireTrades</c> logic in the Firebase app).
/// </summary>
public sealed class TradeExpirationHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TradeExpirationHostedService> _logger;

    public TradeExpirationHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<TradeExpirationHostedService> logger)
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
                await using var scope = _scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<ITradesService>();
                var expired = await service.ExpireOverdueAsync(stoppingToken);

                if (expired > 0)
                {
                    _logger.LogInformation("Expired {Count} overdue trade proposal(s).", expired);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to expire overdue trades.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
