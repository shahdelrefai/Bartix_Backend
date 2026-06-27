using Bartrix.BuildingBlocks.Wallet;
using Bartrix.Modules.Wallet.Contracts;
using Bartrix.Modules.Wallet.Domain;
using Bartrix.Modules.Wallet.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Bartrix.Modules.Wallet.Application;

public sealed class WalletService : IWalletService
{
    private readonly WalletDbContext _dbContext;
    private readonly NpgsqlDataSource _dataSource;
    private readonly IWalletRealtimeNotifier _notifier;
    private readonly TimeProvider _timeProvider;

    public WalletService(
        WalletDbContext dbContext,
        NpgsqlDataSource dataSource,
        IWalletRealtimeNotifier notifier,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _dataSource = dataSource;
        _notifier = notifier;
        _timeProvider = timeProvider;
    }

    public async Task<decimal> GetBalanceAsync(Guid userId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT wallet_balance FROM auth.user_accounts WHERE id = @userId;";
        await using var cmd = _dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("userId", userId);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is decimal d ? d : 0m;
    }

    public async Task CreditAsync(Guid userId, decimal amount, string referenceId, string description, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);

        // Update balance atomically and return new balance
        const string updateSql = """
            UPDATE auth.user_accounts
            SET wallet_balance = wallet_balance + @amount
            WHERE id = @userId
            RETURNING wallet_balance;
            """;

        await using var updateCmd = conn.CreateCommand();
        updateCmd.Transaction = tx;
        updateCmd.CommandText = updateSql;
        updateCmd.Parameters.AddWithValue("userId", userId);
        updateCmd.Parameters.AddWithValue("amount", amount);
        var newBalance = (decimal)(await updateCmd.ExecuteScalarAsync(cancellationToken) ?? throw new WalletValidationException("User not found."));

        // Insert transaction record
        const string insertSql = """
            INSERT INTO wallet.transactions (id, user_id, amount, type, reference_id, description, created_at_utc)
            VALUES (@id, @userId, @amount, @type, @referenceId, @description, @createdAt);
            """;

        await using var insertCmd = conn.CreateCommand();
        insertCmd.Transaction = tx;
        insertCmd.CommandText = insertSql;
        insertCmd.Parameters.AddWithValue("id", Guid.NewGuid());
        insertCmd.Parameters.AddWithValue("userId", userId);
        insertCmd.Parameters.AddWithValue("amount", amount);
        insertCmd.Parameters.AddWithValue("type", WalletTransaction.Types.Credit);
        insertCmd.Parameters.AddWithValue("referenceId", referenceId);
        insertCmd.Parameters.AddWithValue("description", description);
        insertCmd.Parameters.AddWithValue("createdAt", _timeProvider.GetUtcNow());
        await insertCmd.ExecuteNonQueryAsync(cancellationToken);

        await tx.CommitAsync(cancellationToken);

        await _notifier.NotifyBalanceUpdatedAsync(userId, newBalance, cancellationToken);
    }

    public async Task DebitAsync(Guid userId, decimal amount, string referenceId, string description, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);

        // Check and deduct balance — fail if insufficient
        const string updateSql = """
            UPDATE auth.user_accounts
            SET wallet_balance = wallet_balance - @amount
            WHERE id = @userId AND wallet_balance >= @amount
            RETURNING wallet_balance;
            """;

        await using var updateCmd = conn.CreateCommand();
        updateCmd.Transaction = tx;
        updateCmd.CommandText = updateSql;
        updateCmd.Parameters.AddWithValue("userId", userId);
        updateCmd.Parameters.AddWithValue("amount", amount);
        var result = await updateCmd.ExecuteScalarAsync(cancellationToken);

        if (result is null)
        {
            await tx.RollbackAsync(cancellationToken);
            throw new WalletValidationException("Insufficient balance.");
        }

        var newBalance = (decimal)result;

        const string insertSql = """
            INSERT INTO wallet.transactions (id, user_id, amount, type, reference_id, description, created_at_utc)
            VALUES (@id, @userId, @amount, @type, @referenceId, @description, @createdAt);
            """;

        await using var insertCmd = conn.CreateCommand();
        insertCmd.Transaction = tx;
        insertCmd.CommandText = insertSql;
        insertCmd.Parameters.AddWithValue("id", Guid.NewGuid());
        insertCmd.Parameters.AddWithValue("userId", userId);
        insertCmd.Parameters.AddWithValue("amount", amount);
        insertCmd.Parameters.AddWithValue("type", WalletTransaction.Types.Debit);
        insertCmd.Parameters.AddWithValue("referenceId", referenceId);
        insertCmd.Parameters.AddWithValue("description", description);
        insertCmd.Parameters.AddWithValue("createdAt", _timeProvider.GetUtcNow());
        await insertCmd.ExecuteNonQueryAsync(cancellationToken);

        await tx.CommitAsync(cancellationToken);

        await _notifier.NotifyBalanceUpdatedAsync(userId, newBalance, cancellationToken);
    }

    public async Task<IReadOnlyList<WalletTransactionResponse>> GetTransactionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var items = await _dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        return items.Select(t => new WalletTransactionResponse(
            t.Id, t.UserId, t.Amount, t.Type, t.ReferenceId, t.Description, t.CreatedAtUtc)).ToList();
    }
}
