using Bartrix.Modules.Wallet.Application;
using Microsoft.AspNetCore.SignalR;

namespace Bartrix.Api.Hubs;

internal sealed class SignalRWalletRealtimeNotifier(IHubContext<TradeChatHub> hubContext) : IWalletRealtimeNotifier
{
    public Task NotifyBalanceUpdatedAsync(Guid userId, decimal newBalance, CancellationToken cancellationToken)
    {
        var group = TradeChatHub.ToUserGroup(userId.ToString());
        return hubContext.Clients.Group(group)
            .SendAsync("walletBalanceUpdated", new { balance = newBalance }, cancellationToken);
    }
}
