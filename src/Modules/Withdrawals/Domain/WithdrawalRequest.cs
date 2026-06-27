namespace Bartrix.Modules.Withdrawals.Domain;

public sealed class WithdrawalRequest
{
    public Guid Id { get; private set; }
    public Guid SellerId { get; private set; }
    public decimal Amount { get; private set; }
    public string Status { get; private set; } = Statuses.Pending;
    public string? BankDetails { get; private set; }
    public string? AdminNote { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private WithdrawalRequest() { }

    public WithdrawalRequest(Guid sellerId, decimal amount, string? bankDetails, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        SellerId = sellerId;
        Amount = amount;
        BankDetails = bankDetails;
        Status = Statuses.Pending;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void Complete(string? adminNote, DateTimeOffset now)
    {
        Status = Statuses.Completed;
        AdminNote = adminNote;
        UpdatedAtUtc = now;
    }

    public void Reject(string? adminNote, DateTimeOffset now)
    {
        Status = Statuses.Rejected;
        AdminNote = adminNote;
        UpdatedAtUtc = now;
    }

    public static class Statuses
    {
        public const string Pending = "pending";
        public const string Completed = "completed";
        public const string Rejected = "rejected";
    }
}
