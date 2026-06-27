namespace Bartrix.Modules.Trades.Domain;

/// <summary>Append-only audit entry for a trade (mirrors Firebase TradeHistory collection).</summary>
public sealed class TradeHistoryEntry
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid TradeProposalId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public Guid PerformedByUserId { get; private set; }

    public string? PerformedByUserName { get; private set; }

    public string? Details { get; private set; }

    public DateTimeOffset TimestampUtc { get; private set; }

    private TradeHistoryEntry()
    {
    }

    public TradeHistoryEntry(
        Guid tradeProposalId,
        string action,
        Guid performedByUserId,
        string? performedByUserName,
        string? details,
        DateTimeOffset timestampUtc)
    {
        TradeProposalId = tradeProposalId;
        Action = action;
        PerformedByUserId = performedByUserId;
        PerformedByUserName = performedByUserName;
        Details = details;
        TimestampUtc = timestampUtc;
    }
}

public static class TradeHistoryActions
{
    public const string Created = "TRADE_CREATED";
    public const string StatusChanged = "STATUS_CHANGED";
    public const string CounterOfferAdded = "COUNTER_OFFER_ADDED";
    public const string CounterOfferAccepted = "COUNTER_OFFER_ACCEPTED";
    public const string AutoRejected = "AUTO_REJECTED";
    public const string Expired = "TRADE_EXPIRED";
    public const string Completed = "TRADE_COMPLETED";
}
