namespace Bartrix.Modules.Trades.Domain;

public sealed class TradeProposal
{
    private readonly List<TradeProposalOfferedListing> _offeredListings = new();

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid SenderUserId { get; private set; }

    public Guid ReceiverUserId { get; private set; }

    public Guid RequestedListingId { get; private set; }

    public string? Message { get; private set; }

    public TradeStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<TradeProposalOfferedListing> OfferedListings => _offeredListings;

    private TradeProposal()
    {
    }

    public TradeProposal(
        Guid senderUserId,
        Guid receiverUserId,
        Guid requestedListingId,
        string? message,
        DateTimeOffset createdAtUtc)
    {
        SenderUserId = senderUserId;
        ReceiverUserId = receiverUserId;
        RequestedListingId = requestedListingId;
        Message = message;
        Status = TradeStatus.Pending;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public void ReplaceOfferedListings(IEnumerable<Guid> listingIds)
    {
        _offeredListings.Clear();

        foreach (var listingId in listingIds.Distinct())
        {
            _offeredListings.Add(new TradeProposalOfferedListing(Id, listingId));
        }
    }

    public void Accept(Guid actingUserId, DateTimeOffset updatedAtUtc)
    {
        EnsureReceiver(actingUserId);
        EnsurePending();
        Status = TradeStatus.Accepted;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Reject(Guid actingUserId, DateTimeOffset updatedAtUtc)
    {
        EnsureReceiver(actingUserId);
        EnsurePending();
        Status = TradeStatus.Rejected;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Cancel(Guid actingUserId, DateTimeOffset updatedAtUtc)
    {
        if (actingUserId != SenderUserId)
        {
            throw new InvalidOperationException("Only the sender can cancel a trade.");
        }

        EnsurePending();
        Status = TradeStatus.Cancelled;
        UpdatedAtUtc = updatedAtUtc;
    }

    private void EnsureReceiver(Guid actingUserId)
    {
        if (actingUserId != ReceiverUserId)
        {
            throw new InvalidOperationException("Only the receiver can perform this action.");
        }
    }

    private void EnsurePending()
    {
        if (Status != TradeStatus.Pending)
        {
            throw new InvalidOperationException("Trade is no longer pending.");
        }
    }
}
