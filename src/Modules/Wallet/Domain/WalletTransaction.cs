namespace Bartrix.Modules.Wallet.Domain;

public sealed class WalletTransaction
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Amount { get; private set; }
    public string Type { get; private set; } = default!;
    public string ReferenceId { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private WalletTransaction() { }

    public WalletTransaction(Guid userId, decimal amount, string type, string referenceId, string description, DateTimeOffset createdAtUtc)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Amount = amount;
        Type = type;
        ReferenceId = referenceId;
        Description = description;
        CreatedAtUtc = createdAtUtc;
    }

    public static class Types
    {
        public const string Credit = "credit";
        public const string Debit = "debit";
    }
}
