using Bartrix.Modules.Trades.Contracts;
using Bartrix.Modules.Trades.Domain;
using Bartrix.Modules.Trades.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Trades.Application;

public sealed class TradesService : ITradesService
{
    private readonly TradesDbContext _dbContext;
    private readonly IListingTradeValidationReader _listingReader;
    private readonly TimeProvider _timeProvider;

    public TradesService(
        TradesDbContext dbContext,
        IListingTradeValidationReader listingReader,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _listingReader = listingReader;
        _timeProvider = timeProvider;
    }

    public async Task<TradeSummaryResponse> CreateAsync(Guid senderUserId, CreateTradeProposalRequest request, CancellationToken cancellationToken)
    {
        var offeredIds = request.OfferedListingIds?
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList() ?? new List<Guid>();

        if (offeredIds.Count == 0)
        {
            throw new TradesValidationException("At least one offered listing is required.");
        }

        var requestedListing = await _listingReader.GetListingAsync(request.RequestedListingId, cancellationToken);
        if (requestedListing is null || !requestedListing.IsActive)
        {
            throw new TradesValidationException("Requested listing is not available.");
        }

        if (requestedListing.OwnerUserId == senderUserId)
        {
            throw new TradesValidationException("You cannot create a trade against your own listing.");
        }

        var offeredListings = await _listingReader.GetListingsAsync(offeredIds, cancellationToken);
        if (offeredListings.Count != offeredIds.Count)
        {
            throw new TradesValidationException("One or more offered listings were not found.");
        }

        if (offeredListings.Any(x => !x.IsActive))
        {
            throw new TradesValidationException("All offered listings must be active.");
        }

        if (offeredListings.Any(x => x.OwnerUserId != senderUserId))
        {
            throw new TradesValidationException("You can only offer your own listings.");
        }

        var nowUtc = _timeProvider.GetUtcNow();
        var proposal = new TradeProposal(
            senderUserId,
            requestedListing.OwnerUserId,
            request.RequestedListingId,
            NormalizeMessage(request.Message),
            nowUtc);

        proposal.ReplaceOfferedListings(offeredIds);
        _dbContext.TradeProposals.Add(proposal);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Map(proposal);
    }

    public async Task<MyTradesResponse> GetMyTradesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var sent = await _dbContext.TradeProposals
            .AsNoTracking()
            .Include(x => x.OfferedListings)
            .Where(x => x.SenderUserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var received = await _dbContext.TradeProposals
            .AsNoTracking()
            .Include(x => x.OfferedListings)
            .Where(x => x.ReceiverUserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return new MyTradesResponse(sent.Select(Map).ToList(), received.Select(Map).ToList());
    }

    public async Task<TradeSummaryResponse?> GetByIdAsync(Guid userId, Guid tradeId, CancellationToken cancellationToken)
    {
        var trade = await _dbContext.TradeProposals
            .AsNoTracking()
            .Include(x => x.OfferedListings)
            .SingleOrDefaultAsync(x => x.Id == tradeId, cancellationToken);

        if (trade is null)
        {
            return null;
        }

        EnsureParticipant(userId, trade);
        return Map(trade);
    }

    public async Task<TradeSummaryResponse> AcceptAsync(Guid userId, Guid tradeId, CancellationToken cancellationToken)
    {
        var trade = await GetTradeForUpdate(tradeId, cancellationToken);
        ExecuteStatusChange(() => trade.Accept(userId, _timeProvider.GetUtcNow()));
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(trade);
    }

    public async Task<TradeSummaryResponse> RejectAsync(Guid userId, Guid tradeId, CancellationToken cancellationToken)
    {
        var trade = await GetTradeForUpdate(tradeId, cancellationToken);
        ExecuteStatusChange(() => trade.Reject(userId, _timeProvider.GetUtcNow()));
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(trade);
    }

    public async Task CancelAsync(Guid userId, Guid tradeId, CancellationToken cancellationToken)
    {
        var trade = await GetTradeForUpdate(tradeId, cancellationToken);
        ExecuteStatusChange(() => trade.Cancel(userId, _timeProvider.GetUtcNow()));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<TradeProposal> GetTradeForUpdate(Guid tradeId, CancellationToken cancellationToken)
    {
        var trade = await _dbContext.TradeProposals
            .Include(x => x.OfferedListings)
            .SingleOrDefaultAsync(x => x.Id == tradeId, cancellationToken);

        if (trade is null)
        {
            throw new TradesValidationException("Trade proposal was not found.");
        }

        return trade;
    }

    private static void EnsureParticipant(Guid userId, TradeProposal trade)
    {
        if (trade.SenderUserId != userId && trade.ReceiverUserId != userId)
        {
            throw new TradesValidationException("You do not have access to this trade.");
        }
    }

    private static string? NormalizeMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        var normalized = message.Trim();
        if (normalized.Length > 1000)
        {
            throw new TradesValidationException("Message cannot exceed 1000 characters.");
        }

        return normalized;
    }

    private static void ExecuteStatusChange(Action action)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException exception)
        {
            throw new TradesValidationException(exception.Message);
        }
    }

    private static TradeSummaryResponse Map(TradeProposal trade)
    {
        return new TradeSummaryResponse(
            trade.Id,
            trade.SenderUserId,
            trade.ReceiverUserId,
            trade.RequestedListingId,
            trade.OfferedListings.Select(x => x.ListingId).ToList(),
            trade.Message,
            trade.Status.ToString(),
            trade.CreatedAtUtc,
            trade.UpdatedAtUtc);
    }
}
