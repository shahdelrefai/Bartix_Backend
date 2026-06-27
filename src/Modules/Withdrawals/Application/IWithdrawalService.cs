using Bartrix.Modules.Withdrawals.Contracts;

namespace Bartrix.Modules.Withdrawals.Application;

public interface IWithdrawalService
{
    Task<WithdrawalResponse> CreateAsync(Guid sellerId, CreateWithdrawalRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<WithdrawalResponse>> GetForSellerAsync(Guid sellerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<WithdrawalResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<WithdrawalResponse> UpdateStatusAsync(Guid requestId, UpdateWithdrawalStatusRequest request, CancellationToken cancellationToken);
}
