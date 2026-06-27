namespace Bartrix.Modules.Withdrawals.Application;

public sealed class WithdrawalValidationException(string message) : Exception(message);
