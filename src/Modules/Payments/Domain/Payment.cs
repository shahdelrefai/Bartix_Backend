namespace Bartrix.Modules.Payments.Domain;

public sealed class Payment
{
    public Guid Id { get; private set; }
    public Guid BuyerId { get; private set; }
    public Guid SellerId { get; private set; }
    public string ProductTitle { get; private set; } = default!;
    public decimal Amount { get; private set; }
    public decimal FeeAmount { get; private set; }
    public string Status { get; private set; } = Statuses.Pending;
    public string? PaymobTransactionId { get; private set; }
    public bool IsCredited { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private Payment() { }

    public Payment(Guid buyerId, Guid sellerId, string productTitle, decimal amount, decimal feeAmount, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        BuyerId = buyerId;
        SellerId = sellerId;
        ProductTitle = productTitle;
        Amount = amount;
        FeeAmount = feeAmount;
        Status = Statuses.Pending;
        IsCredited = false;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void Complete(string? paymobTransactionId, DateTimeOffset now)
    {
        Status = Statuses.Completed;
        PaymobTransactionId = paymobTransactionId;
        UpdatedAtUtc = now;
    }

    public void Fail(DateTimeOffset now)
    {
        Status = Statuses.Failed;
        UpdatedAtUtc = now;
    }

    public void SetFeeAmount(decimal fee)
    {
        FeeAmount = fee;
    }

    public void MarkCredited(DateTimeOffset now)
    {
        IsCredited = true;
        UpdatedAtUtc = now;
    }

    public static class Statuses
    {
        public const string Pending = "pending";
        public const string Completed = "completed";
        public const string Failed = "failed";
    }
}
