using Bartrix.BuildingBlocks.Listings;
using Bartrix.Modules.Trades.Contracts;
using Bartrix.Modules.Trades.Domain;
using Bartrix.Modules.Trades.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Trades.Application;

public sealed class TradesService : ITradesService
{
    private const int DefaultExpiryDays = 7;

    private readonly TradesDbContext _dbContext;
    private readonly IListingTradeValidationReader _listingReader;
    private readonly IListingStatusWriter _listingStatusWriter;
    private readonly TimeProvider _timeProvider;

    public TradesService(
        TradesDbContext dbContext,
        IListingTradeValidationReader listingReader,
        IListingStatusWriter listingStatusWriter,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _listingReader = listingReader;
        _listingStatusWriter = listingStatusWriter;
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
        var expiresAtUtc = nowUtc.AddHours(request.ExpiresInHours is > 0 ? request.ExpiresInHours.Value : DefaultExpiryDays * 24);

        var proposal = new TradeProposal(
            senderUserId,
            requestedListing.OwnerUserId,
            request.RequestedListingId,
            NormalizeMessage(request.Message),
            nowUtc,
            expiresAtUtc);

        proposal.SetMetadata(
            request.SenderUserName,
            request.ReceiverUserName,
            request.Type,
            request.IsFromPremium,
            isCounterOffer: false,
            parentTradeId: null,
            request.RequestedListingIds);

        proposal.ReplaceOfferedListings(offeredIds);
        _dbContext.TradeProposals.Add(proposal);
        AddHistory(proposal, TradeHistoryActions.Created, senderUserId, request.SenderUserName, "Trade created", nowUtc);
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

        return new MyTradesResponse(sent.Select(x => Map(x)).ToList(), received.Select(x => Map(x)).ToList());
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
        var counterOffers = await LoadCounterOffersAsync(tradeId, cancellationToken);
        return Map(trade, counterOffers);
    }

    public async Task<TradeSummaryResponse> AcceptAsync(Guid userId, Guid tradeId, CancellationToken cancellationToken)
    {
        var trade = await GetTradeForUpdate(tradeId, cancellationToken);
        var nowUtc = _timeProvider.GetUtcNow();
        ExecuteStatusChange(() => trade.Accept(userId, nowUtc));
        AddHistory(trade, TradeHistoryActions.StatusChanged, userId, ActorName(trade, userId), "Trade accepted", nowUtc);

        await AutoRejectCompetingTradesAsync(trade, nowUtc, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Mark requested listings as reserved so they don't appear available
        var reservedIds = trade.RequestedListingIds.Count > 0
            ? trade.RequestedListingIds.ToList()
            : new List<Guid> { trade.RequestedListingId };
        await _listingStatusWriter.SetManyStatusAsync(reservedIds, "reserved", cancellationToken);

        return Map(trade);
    }

    public async Task<TradeSummaryResponse> RejectAsync(Guid userId, Guid tradeId, RejectTradeRequest request, CancellationToken cancellationToken)
    {
        var trade = await GetTradeForUpdate(tradeId, cancellationToken);
        var nowUtc = _timeProvider.GetUtcNow();
        ExecuteStatusChange(() => trade.Reject(userId, request.Reason, nowUtc));
        AddHistory(trade, TradeHistoryActions.StatusChanged, userId, ActorName(trade, userId),
            request.Reason is null ? "Trade rejected" : $"Trade rejected: {request.Reason}", nowUtc);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(trade);
    }

    public async Task CancelAsync(Guid userId, Guid tradeId, CancellationToken cancellationToken)
    {
        var trade = await GetTradeForUpdate(tradeId, cancellationToken);
        var nowUtc = _timeProvider.GetUtcNow();
        ExecuteStatusChange(() => trade.Cancel(userId, nowUtc));
        AddHistory(trade, TradeHistoryActions.StatusChanged, userId, ActorName(trade, userId), "Trade cancelled", nowUtc);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TradeSummaryResponse> CompleteAsync(Guid userId, Guid tradeId, CancellationToken cancellationToken)
    {
        var trade = await GetTradeForUpdate(tradeId, cancellationToken);
        var nowUtc = _timeProvider.GetUtcNow();
        ExecuteStatusChange(() => trade.Complete(userId, nowUtc));
        AddHistory(trade, TradeHistoryActions.Completed, userId, ActorName(trade, userId), "Trade completed", nowUtc);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Mark all involved listings as traded
        var allListingIds = trade.OfferedListings.Select(x => x.ListingId).ToList();
        var requestedIds = trade.RequestedListingIds.Count > 0
            ? trade.RequestedListingIds.ToList()
            : new List<Guid> { trade.RequestedListingId };
        allListingIds.AddRange(requestedIds);
        await _listingStatusWriter.SetManyStatusAsync(allListingIds.Distinct().ToList(), "traded", cancellationToken);

        return Map(trade);
    }

    public async Task<TradeCounterOfferResponse> AddCounterOfferAsync(Guid userId, Guid tradeId, AddCounterOfferRequest request, CancellationToken cancellationToken)
    {
        var trade = await GetTradeForUpdate(tradeId, cancellationToken);
        EnsureParticipant(userId, trade);

        if (trade.Status != TradeStatus.Pending)
        {
            throw new TradesValidationException("Counter-offers can only be added to a pending trade.");
        }

        var offeredIds = (request.OfferedListingIds ?? Array.Empty<Guid>()).Where(x => x != Guid.Empty).Distinct().ToList();
        var requestedIds = (request.RequestedListingIds ?? Array.Empty<Guid>()).Where(x => x != Guid.Empty).Distinct().ToList();

        if (offeredIds.Count == 0)
        {
            throw new TradesValidationException("A counter-offer must include at least one offered listing.");
        }

        var toUserId = userId == trade.SenderUserId ? trade.ReceiverUserId : trade.SenderUserId;

        var offered = await _listingReader.GetListingsAsync(offeredIds, cancellationToken);
        if (offered.Count != offeredIds.Count || offered.Any(x => !x.IsActive) || offered.Any(x => x.OwnerUserId != userId))
        {
            throw new TradesValidationException("You can only offer your own active listings in a counter-offer.");
        }

        if (requestedIds.Count > 0)
        {
            var requested = await _listingReader.GetListingsAsync(requestedIds, cancellationToken);
            if (requested.Count != requestedIds.Count || requested.Any(x => x.OwnerUserId != toUserId))
            {
                throw new TradesValidationException("Requested listings in a counter-offer must belong to the other participant.");
            }
        }

        var nowUtc = _timeProvider.GetUtcNow();
        var counterOffer = new TradeCounterOffer(
            tradeId,
            userId,
            toUserId,
            offeredIds,
            requestedIds,
            NormalizeMessage(request.Message),
            nowUtc);

        _dbContext.TradeCounterOffers.Add(counterOffer);
        AddHistory(trade, TradeHistoryActions.CounterOfferAdded, userId, ActorName(trade, userId), "Counter-offer added", nowUtc);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapCounterOffer(counterOffer);
    }

    public async Task<TradeSummaryResponse> AcceptCounterOfferAsync(Guid userId, Guid tradeId, Guid counterOfferId, CancellationToken cancellationToken)
    {
        var trade = await GetTradeForUpdate(tradeId, cancellationToken);
        var counterOffer = await _dbContext.TradeCounterOffers
            .SingleOrDefaultAsync(x => x.Id == counterOfferId && x.TradeProposalId == tradeId, cancellationToken)
            ?? throw new TradesValidationException("Counter-offer was not found.");

        if (counterOffer.ToUserId != userId)
        {
            throw new TradesValidationException("Only the counter-offer recipient can accept it.");
        }

        if (trade.Status != TradeStatus.Pending)
        {
            throw new TradesValidationException("Trade is no longer pending.");
        }

        var nowUtc = _timeProvider.GetUtcNow();
        counterOffer.MarkAccepted();
        // Accepting a counter-offer settles the parent trade in the receiver's favour.
        ExecuteStatusChange(() => trade.Accept(trade.ReceiverUserId, nowUtc));
        AddHistory(trade, TradeHistoryActions.CounterOfferAccepted, userId, ActorName(trade, userId), "Counter-offer accepted", nowUtc);

        await AutoRejectCompetingTradesAsync(trade, nowUtc, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var reservedIds = trade.RequestedListingIds.Count > 0
            ? trade.RequestedListingIds.ToList()
            : new List<Guid> { trade.RequestedListingId };
        await _listingStatusWriter.SetManyStatusAsync(reservedIds, "reserved", cancellationToken);

        return Map(trade);
    }

    public async Task<IReadOnlyList<TradeCounterOfferResponse>> GetCounterOffersAsync(Guid userId, Guid tradeId, CancellationToken cancellationToken)
    {
        var trade = await _dbContext.TradeProposals.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == tradeId, cancellationToken)
            ?? throw new TradesValidationException("Trade proposal was not found.");

        EnsureParticipant(userId, trade);
        return await LoadCounterOffersAsync(tradeId, cancellationToken);
    }

    public async Task<IReadOnlyList<TradeHistoryResponse>> GetHistoryAsync(Guid userId, Guid tradeId, CancellationToken cancellationToken)
    {
        var trade = await _dbContext.TradeProposals.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == tradeId, cancellationToken)
            ?? throw new TradesValidationException("Trade proposal was not found.");

        EnsureParticipant(userId, trade);

        var entries = await _dbContext.TradeHistory.AsNoTracking()
            .Where(x => x.TradeProposalId == tradeId)
            .OrderBy(x => x.TimestampUtc)
            .ToListAsync(cancellationToken);

        return entries.Select(x => new TradeHistoryResponse(
            x.Id, x.TradeProposalId, x.Action, x.PerformedByUserId, x.PerformedByUserName, x.Details, x.TimestampUtc)).ToList();
    }

    public async Task<int> ExpireOverdueAsync(CancellationToken cancellationToken)
    {
        var nowUtc = _timeProvider.GetUtcNow();
        var overdue = await _dbContext.TradeProposals
            .Where(x => x.Status == TradeStatus.Pending && x.ExpiresAtUtc < nowUtc)
            .ToListAsync(cancellationToken);

        foreach (var trade in overdue)
        {
            trade.Expire(nowUtc);
            AddHistory(trade, TradeHistoryActions.Expired, trade.SenderUserId, trade.SenderUserName, "Trade expired", nowUtc);
        }

        if (overdue.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return overdue.Count;
    }

    private async Task AutoRejectCompetingTradesAsync(TradeProposal acceptedTrade, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        var competing = await _dbContext.TradeProposals
            .Where(x => x.Id != acceptedTrade.Id
                && x.Status == TradeStatus.Pending
                && x.RequestedListingId == acceptedTrade.RequestedListingId)
            .ToListAsync(cancellationToken);

        foreach (var trade in competing)
        {
            trade.AutoReject("Another trade for this item was accepted.", nowUtc);
            AddHistory(trade, TradeHistoryActions.AutoRejected, acceptedTrade.ReceiverUserId,
                ActorName(acceptedTrade, acceptedTrade.ReceiverUserId), "Auto-rejected due to a competing accepted trade", nowUtc);
        }
    }

    private async Task<IReadOnlyList<TradeCounterOfferResponse>> LoadCounterOffersAsync(Guid tradeId, CancellationToken cancellationToken)
    {
        var counterOffers = await _dbContext.TradeCounterOffers.AsNoTracking()
            .Where(x => x.TradeProposalId == tradeId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return counterOffers.Select(MapCounterOffer).ToList();
    }

    private void AddHistory(TradeProposal trade, string action, Guid performedBy, string? performedByName, string details, DateTimeOffset timestampUtc)
    {
        _dbContext.TradeHistory.Add(new TradeHistoryEntry(trade.Id, action, performedBy, performedByName, details, timestampUtc));
    }

    private static string? ActorName(TradeProposal trade, Guid userId)
    {
        if (userId == trade.SenderUserId)
        {
            return trade.SenderUserName;
        }

        return userId == trade.ReceiverUserId ? trade.ReceiverUserName : null;
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

    private static TradeCounterOfferResponse MapCounterOffer(TradeCounterOffer counterOffer)
    {
        return new TradeCounterOfferResponse(
            counterOffer.Id,
            counterOffer.TradeProposalId,
            counterOffer.FromUserId,
            counterOffer.ToUserId,
            counterOffer.OfferedListingIds.ToList(),
            counterOffer.RequestedListingIds.ToList(),
            counterOffer.Message,
            counterOffer.IsAccepted,
            counterOffer.CreatedAtUtc);
    }

    private static TradeSummaryResponse Map(TradeProposal trade, IReadOnlyList<TradeCounterOfferResponse>? counterOffers = null)
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
            trade.UpdatedAtUtc,
            RequestedListingIds: trade.RequestedListingIds.Count > 0 ? trade.RequestedListingIds.ToList() : new List<Guid> { trade.RequestedListingId },
            Type: trade.Type,
            SenderUserName: trade.SenderUserName,
            ReceiverUserName: trade.ReceiverUserName,
            RejectionReason: trade.RejectionReason,
            IsCounterOffer: trade.IsCounterOffer,
            ParentTradeId: trade.ParentTradeId,
            IsFromPremium: trade.IsFromPremium,
            DeliveryProvidedBy: trade.DeliveryProvidedBy.ToList(),
            ExpiresAtUtc: trade.ExpiresAtUtc,
            CounterOffers: counterOffers ?? Array.Empty<TradeCounterOfferResponse>());
    }
}
