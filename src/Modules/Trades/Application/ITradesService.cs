using Bartrix.Modules.Trades.Contracts;

namespace Bartrix.Modules.Trades.Application;

public interface ITradesService
{
    Task<TradeSummaryResponse> CreateAsync(Guid senderUserId, CreateTradeProposalRequest request, CancellationToken cancellationToken);

    Task<MyTradesResponse> GetMyTradesAsync(Guid userId, CancellationToken cancellationToken);

    Task<TradeSummaryResponse?> GetByIdAsync(Guid userId, Guid tradeId, CancellationToken cancellationToken);

    Task<TradeSummaryResponse> AcceptAsync(Guid userId, Guid tradeId, CancellationToken cancellationToken);

    Task<TradeSummaryResponse> RejectAsync(Guid userId, Guid tradeId, CancellationToken cancellationToken);

    Task CancelAsync(Guid userId, Guid tradeId, CancellationToken cancellationToken);
}
