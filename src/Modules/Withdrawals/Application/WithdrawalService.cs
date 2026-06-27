using Bartrix.BuildingBlocks.Notifications;
using Bartrix.BuildingBlocks.Wallet;
using Bartrix.Modules.Withdrawals.Contracts;
using Bartrix.Modules.Withdrawals.Domain;
using Bartrix.Modules.Withdrawals.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Withdrawals.Application;

public sealed class WithdrawalService(
    WithdrawalsDbContext dbContext,
    IWalletService walletService,
    INotificationPublisher notifications,
    TimeProvider timeProvider) : IWithdrawalService
{
    public async Task<WithdrawalResponse> CreateAsync(Guid sellerId, CreateWithdrawalRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            throw new WithdrawalValidationException("Amount must be greater than zero.");

        var balance = await walletService.GetBalanceAsync(sellerId, cancellationToken);
        if (balance < request.Amount)
            throw new WithdrawalValidationException($"Insufficient balance. Available: {balance:F2} EGP.");

        var wr = new WithdrawalRequest(sellerId, request.Amount, request.BankDetails, timeProvider.GetUtcNow());
        dbContext.WithdrawalRequests.Add(wr);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(wr);
    }

    public async Task<IReadOnlyList<WithdrawalResponse>> GetForSellerAsync(Guid sellerId, CancellationToken cancellationToken)
    {
        var items = await dbContext.WithdrawalRequests
            .AsNoTracking()
            .Where(w => w.SellerId == sellerId)
            .OrderByDescending(w => w.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return items.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<WithdrawalResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var items = await dbContext.WithdrawalRequests
            .AsNoTracking()
            .OrderByDescending(w => w.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return items.Select(Map).ToList();
    }

    public async Task<WithdrawalResponse> UpdateStatusAsync(Guid requestId, UpdateWithdrawalStatusRequest request, CancellationToken cancellationToken)
    {
        var wr = await dbContext.WithdrawalRequests
            .SingleOrDefaultAsync(w => w.Id == requestId, cancellationToken)
            ?? throw new WithdrawalValidationException("Withdrawal request not found.");

        if (wr.Status != WithdrawalRequest.Statuses.Pending)
            throw new WithdrawalValidationException($"Cannot update a {wr.Status} withdrawal request.");

        var now = timeProvider.GetUtcNow();

        if (request.Status == WithdrawalRequest.Statuses.Completed)
        {
            // Debit atomically — throws if insufficient balance
            await walletService.DebitAsync(
                wr.SellerId,
                wr.Amount,
                wr.Id.ToString(),
                "Withdrawal completed",
                cancellationToken);

            wr.Complete(request.AdminNote, now);
            await dbContext.SaveChangesAsync(cancellationToken);

            await notifications.PublishAsync(new NotificationPublishRequest(
                wr.SellerId,
                TitleKey: "withdrawalCompletedTitle",
                BodyKey: "withdrawalCompletedBody",
                Type: "wallet_update",
                BodyArgs: new Dictionary<string, string> { ["amount"] = wr.Amount.ToString("F2") },
                RelatedId: wr.Id.ToString()), cancellationToken);
        }
        else if (request.Status == WithdrawalRequest.Statuses.Rejected)
        {
            wr.Reject(request.AdminNote, now);
            await dbContext.SaveChangesAsync(cancellationToken);

            await notifications.PublishAsync(new NotificationPublishRequest(
                wr.SellerId,
                TitleKey: "withdrawalRejectedTitle",
                BodyKey: "withdrawalRejectedBody",
                Type: "wallet_update",
                BodyArgs: new Dictionary<string, string> { ["amount"] = wr.Amount.ToString("F2") },
                RelatedId: wr.Id.ToString()), cancellationToken);
        }
        else
        {
            throw new WithdrawalValidationException($"Invalid status '{request.Status}'. Must be 'completed' or 'rejected'.");
        }

        return Map(wr);
    }

    private static WithdrawalResponse Map(WithdrawalRequest w) =>
        new(w.Id, w.SellerId, w.Amount, w.Status, w.BankDetails, w.AdminNote, w.CreatedAtUtc, w.UpdatedAtUtc);
}
