namespace Bartrix.Modules.Wallet.Application;

public sealed class WalletValidationException(string message) : Exception(message);
