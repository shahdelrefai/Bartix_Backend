namespace Bartrix.Modules.Withdrawals.Contracts;

public sealed record CreateWithdrawalRequest(decimal Amount, string? BankDetails);
