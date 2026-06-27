namespace Bartrix.BuildingBlocks.Wallet;

/// <summary>
/// Cross-cutting port used by Payments and Withdrawals modules to update the
/// user's wallet balance. Implemented by the Wallet module.
/// </summary>
public interface IWalletService
{
    Task<decimal> GetBalanceAsync(Guid userId, CancellationToken cancellationToken);
    Task CreditAsync(Guid userId, decimal amount, string referenceId, string description, CancellationToken cancellationToken);
    Task DebitAsync(Guid userId, decimal amount, string referenceId, string description, CancellationToken cancellationToken);
}
