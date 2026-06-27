using Bartrix.Modules.Wallet.Application;

namespace Bartrix.Modules.Wallet.Infrastructure;

internal sealed class NoOpWalletRealtimeNotifier : IWalletRealtimeNotifier
{
    public Task NotifyBalanceUpdatedAsync(Guid userId, decimal newBalance, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
