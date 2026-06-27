namespace Bartrix.Modules.Withdrawals.Contracts;

public sealed record UpdateWithdrawalStatusRequest(string Status, string? AdminNote);
