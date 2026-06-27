namespace Bartrix.Modules.Wallet.Application;

public interface IWalletRealtimeNotifier
{
    Task NotifyBalanceUpdatedAsync(Guid userId, decimal newBalance, CancellationToken cancellationToken);
}
