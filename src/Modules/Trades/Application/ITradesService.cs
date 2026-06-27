using Bartrix.Modules.Trades.Contracts;

namespace Bartrix.Modules.Trades.Application;

public interface ITradesService
{
    Task<TradeSummaryResponse> CreateAsync(Guid senderUserId, CreateTradeProposalRequest request, CancellationToken cancellationToken);

    Task<MyTradesResponse> GetMyTradesAsync(Guid userId, CancellationToken cancellationToken);

    Task<TradeSummaryResponse?> GetByIdAsync(Guid userId, Guid tradeId, CancellationToken cancellationToken);

    Task<TradeSummaryResponse> AcceptAsync(Guid userId, Guid tradeId, CancellationToken cancellationToken);

    Task<TradeSummaryResponse> RejectAsync(Guid userId, Guid tradeId, RejectTradeRequest request, CancellationToken cancellationToken);

    Task CancelAsync(Guid userId, Guid tradeId, CancellationToken cancellationToken);

    Task<TradeSummaryResponse> CompleteAsync(Guid userId, Guid tradeId, CancellationToken cancellationToken);

    // ─── Counter-offers ─────────────────────────────────────────────────
    Task<TradeCounterOfferResponse> AddCounterOfferAsync(Guid userId, Guid tradeId, AddCounterOfferRequest request, CancellationToken cancellationToken);

    Task<TradeSummaryResponse> AcceptCounterOfferAsync(Guid userId, Guid tradeId, Guid counterOfferId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TradeCounterOfferResponse>> GetCounterOffersAsync(Guid userId, Guid tradeId, CancellationToken cancellationToken);

    // ─── History ────────────────────────────────────────────────────────
    Task<IReadOnlyList<TradeHistoryResponse>> GetHistoryAsync(Guid userId, Guid tradeId, CancellationToken cancellationToken);

    // ─── Expiration (used by the background hosted service) ──────────────
    Task<int> ExpireOverdueAsync(CancellationToken cancellationToken);
}
