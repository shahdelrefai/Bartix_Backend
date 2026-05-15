namespace Bartrix.Modules.Delivery.Domain;

public sealed class TradeDelivery
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid TradeProposalId { get; private set; }

    public Guid ParticipantAUserId { get; private set; }

    public Guid ParticipantBUserId { get; private set; }

    public DeliveryMethod? Method { get; private set; }

    public DeliveryStatus Status { get; private set; }

    public string? Location { get; private set; }

    public DateTimeOffset? ScheduledAtUtc { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private TradeDelivery()
    {
    }

    public TradeDelivery(
        Guid tradeProposalId,
        Guid participantAUserId,
        Guid participantBUserId,
        DateTimeOffset createdAtUtc)
    {
        TradeProposalId = tradeProposalId;
        ParticipantAUserId = participantAUserId;
        ParticipantBUserId = participantBUserId;
        Status = DeliveryStatus.Pending;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public bool HasParticipant(Guid userId)
    {
        return userId == ParticipantAUserId || userId == ParticipantBUserId;
    }

    public void Schedule(
        Guid actingUserId,
        DeliveryMethod method,
        string? location,
        DateTimeOffset? scheduledAtUtc,
        string? notes,
        DateTimeOffset updatedAtUtc)
    {
        EnsureParticipant(actingUserId);

        if (Status == DeliveryStatus.Delivered || Status == DeliveryStatus.Confirmed || Status == DeliveryStatus.Cancelled)
        {
            throw new InvalidOperationException("Delivery can no longer be scheduled.");
        }

        Method = method;
        Location = location;
        ScheduledAtUtc = scheduledAtUtc;
        Notes = notes;
        Status = DeliveryStatus.Scheduled;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void MarkDelivered(Guid actingUserId, DateTimeOffset updatedAtUtc)
    {
        EnsureParticipant(actingUserId);

        if (Status != DeliveryStatus.Scheduled)
        {
            throw new InvalidOperationException("Only scheduled deliveries can be marked delivered.");
        }

        Status = DeliveryStatus.Delivered;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Confirm(Guid actingUserId, DateTimeOffset updatedAtUtc)
    {
        EnsureParticipant(actingUserId);

        if (Status != DeliveryStatus.Delivered)
        {
            throw new InvalidOperationException("Only delivered exchanges can be confirmed.");
        }

        Status = DeliveryStatus.Confirmed;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Cancel(Guid actingUserId, DateTimeOffset updatedAtUtc)
    {
        EnsureParticipant(actingUserId);

        if (Status == DeliveryStatus.Confirmed)
        {
            throw new InvalidOperationException("Confirmed deliveries cannot be cancelled.");
        }

        if (Status == DeliveryStatus.Cancelled)
        {
            throw new InvalidOperationException("Delivery is already cancelled.");
        }

        Status = DeliveryStatus.Cancelled;
        UpdatedAtUtc = updatedAtUtc;
    }

    private void EnsureParticipant(Guid userId)
    {
        if (!HasParticipant(userId))
        {
            throw new InvalidOperationException("Only trade participants can update the delivery.");
        }
    }
}
