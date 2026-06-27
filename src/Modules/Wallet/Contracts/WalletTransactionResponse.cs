namespace Bartrix.Modules.Wallet.Contracts;

public sealed record WalletTransactionResponse(
    Guid Id,
    Guid UserId,
    decimal Amount,
    string Type,
    string ReferenceId,
    string Description,
    DateTimeOffset CreatedAtUtc);
