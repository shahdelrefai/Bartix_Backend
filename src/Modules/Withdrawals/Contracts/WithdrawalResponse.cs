namespace Bartrix.Modules.Withdrawals.Contracts;

public sealed record WithdrawalResponse(
    Guid Id,
    Guid SellerId,
    decimal Amount,
    string Status,
    string? BankDetails,
    string? AdminNote,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
