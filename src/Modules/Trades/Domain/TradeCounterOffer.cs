namespace Bartrix.Modules.Trades.Domain;

/// <summary>A counter-offer attached to a parent trade proposal (mirrors Firebase counterOffers[]).</summary>
public sealed class TradeCounterOffer
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid TradeProposalId { get; private set; }

    public Guid FromUserId { get; private set; }

    public Guid ToUserId { get; private set; }

    public List<Guid> OfferedListingIds { get; private set; } = new();

    public List<Guid> RequestedListingIds { get; private set; } = new();

    public string? Message { get; private set; }

    public bool IsAccepted { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private TradeCounterOffer()
    {
    }

    public TradeCounterOffer(
        Guid tradeProposalId,
        Guid fromUserId,
        Guid toUserId,
        IEnumerable<Guid> offeredListingIds,
        IEnumerable<Guid> requestedListingIds,
        string? message,
        DateTimeOffset createdAtUtc)
    {
        TradeProposalId = tradeProposalId;
        FromUserId = fromUserId;
        ToUserId = toUserId;
        OfferedListingIds = offeredListingIds.Where(x => x != Guid.Empty).Distinct().ToList();
        RequestedListingIds = requestedListingIds.Where(x => x != Guid.Empty).Distinct().ToList();
        Message = message;
        CreatedAtUtc = createdAtUtc;
    }

    public void MarkAccepted()
    {
        IsAccepted = true;
    }
}
