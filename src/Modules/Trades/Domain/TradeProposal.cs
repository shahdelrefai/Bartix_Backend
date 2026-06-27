namespace Bartrix.Modules.Trades.Domain;

public sealed class TradeProposal
{
    private readonly List<TradeProposalOfferedListing> _offeredListings = new();

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid SenderUserId { get; private set; }

    public Guid ReceiverUserId { get; private set; }

    public string? SenderUserName { get; private set; }

    public string? ReceiverUserName { get; private set; }

    public Guid RequestedListingId { get; private set; }

    /// <summary>All requested listing ids (mirrors Firebase requestedProductIds). Includes <see cref="RequestedListingId"/>.</summary>
    public List<Guid> RequestedListingIds { get; private set; } = new();

    public string? Message { get; private set; }

    public TradeStatus Status { get; private set; }

    /// <summary>Mirrors Firebase trade type: itemForItem, serviceForItem, serviceForService, multiForSingle, any.</summary>
    public string Type { get; private set; } = "any";

    public string? RejectionReason { get; private set; }

    public bool IsCounterOffer { get; private set; }

    public Guid? ParentTradeId { get; private set; }

    public bool IsFromPremium { get; private set; }

    public List<Guid> DeliveryProvidedBy { get; private set; } = new();

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public IReadOnlyCollection<TradeProposalOfferedListing> OfferedListings => _offeredListings;

    private TradeProposal()
    {
    }

    public TradeProposal(
        Guid senderUserId,
        Guid receiverUserId,
        Guid requestedListingId,
        string? message,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        SenderUserId = senderUserId;
        ReceiverUserId = receiverUserId;
        RequestedListingId = requestedListingId;
        RequestedListingIds = new List<Guid> { requestedListingId };
        Message = message;
        Status = TradeStatus.Pending;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public void SetMetadata(
        string? senderUserName,
        string? receiverUserName,
        string type,
        bool isFromPremium,
        bool isCounterOffer,
        Guid? parentTradeId,
        IEnumerable<Guid>? requestedListingIds)
    {
        SenderUserName = senderUserName;
        ReceiverUserName = receiverUserName;
        Type = string.IsNullOrWhiteSpace(type) ? "any" : type;
        IsFromPremium = isFromPremium;
        IsCounterOffer = isCounterOffer;
        ParentTradeId = parentTradeId;

        if (requestedListingIds is not null)
        {
            var ids = requestedListingIds.Where(x => x != Guid.Empty).Distinct().ToList();
            if (!ids.Contains(RequestedListingId))
            {
                ids.Insert(0, RequestedListingId);
            }

            RequestedListingIds = ids;
        }
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

    public void Reject(Guid actingUserId, string? reason, DateTimeOffset updatedAtUtc)
    {
        EnsureReceiver(actingUserId);
        EnsurePending();
        Status = TradeStatus.Rejected;
        RejectionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>Automatically reject a competing pending trade (no acting-user check).</summary>
    public void AutoReject(string reason, DateTimeOffset updatedAtUtc)
    {
        EnsurePending();
        Status = TradeStatus.Rejected;
        RejectionReason = reason;
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

    public void Complete(Guid actingUserId, DateTimeOffset updatedAtUtc)
    {
        EnsureParticipant(actingUserId);

        if (Status != TradeStatus.Accepted)
        {
            throw new InvalidOperationException("Only an accepted trade can be completed.");
        }

        Status = TradeStatus.Completed;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Expire(DateTimeOffset updatedAtUtc)
    {
        EnsurePending();
        Status = TradeStatus.Expired;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void AddDeliveryProvidedBy(Guid userId)
    {
        EnsureParticipant(userId);
        if (!DeliveryProvidedBy.Contains(userId))
        {
            DeliveryProvidedBy.Add(userId);
        }
    }

    private void EnsureReceiver(Guid actingUserId)
    {
        if (actingUserId != ReceiverUserId)
        {
            throw new InvalidOperationException("Only the receiver can perform this action.");
        }
    }

    private void EnsureParticipant(Guid actingUserId)
    {
        if (actingUserId != SenderUserId && actingUserId != ReceiverUserId)
        {
            throw new InvalidOperationException("You are not a participant in this trade.");
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
