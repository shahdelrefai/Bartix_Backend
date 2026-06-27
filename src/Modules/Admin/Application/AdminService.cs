using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Admin.Contracts;
using Npgsql;
using NpgsqlTypes;

namespace Bartrix.Modules.Admin.Application;

public sealed class AdminService : IAdminService
{
    private static readonly HashSet<string> ValidRoles = new(StringComparer.OrdinalIgnoreCase)
        { "user", "admin", "moderator", "premium", "agent" };

    private readonly IPostgresConnectionFactory _connectionFactory;
    private readonly TimeProvider _timeProvider;

    public AdminService(IPostgresConnectionFactory connectionFactory, TimeProvider timeProvider)
    {
        _connectionFactory = connectionFactory;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<AdminUserResponse>> GetUsersAsync(string? search, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var sql = """
            SELECT id, display_name, email, role, language_code, profile_image_url,
                   is_suspended, is_premium_active, wallet_balance, created_at_utc
            FROM auth.user_accounts
            WHERE (@search IS NULL OR display_name ILIKE '%' || @search || '%' OR email ILIKE '%' || @search || '%')
            ORDER BY created_at_utc DESC
            LIMIT 500
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.Add(new NpgsqlParameter("search", NpgsqlDbType.Text) { Value = (object?)search ?? DBNull.Value });

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var users = new List<AdminUserResponse>();
        while (await reader.ReadAsync(cancellationToken))
            users.Add(ReadUser(reader));
        return users;
    }

    public async Task<AdminUserResponse> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var sql = """
            SELECT id, display_name, email, role, language_code, profile_image_url,
                   is_suspended, is_premium_active, wallet_balance, created_at_utc
            FROM auth.user_accounts WHERE id = @userId
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userId);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            throw new AdminValidationException("User not found.");
        return ReadUser(reader);
    }

    public async Task SetRoleAsync(Guid userId, string role, CancellationToken cancellationToken)
    {
        if (!ValidRoles.Contains(role))
            throw new AdminValidationException($"Invalid role '{role}'.");

        await using var conn = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(
            "UPDATE auth.user_accounts SET role = @role WHERE id = @userId", conn);
        cmd.Parameters.AddWithValue("role", role.ToLower());
        cmd.Parameters.AddWithValue("userId", userId);

        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        if (rows == 0) throw new AdminValidationException("User not found.");
    }

    public async Task SuspendUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            await ExecuteAsync(conn, tx, cancellationToken,
                "UPDATE auth.user_accounts SET is_suspended = true WHERE id = @userId",
                ("userId", (object)userId));

            await ExecuteAsync(conn, tx, cancellationToken,
                "UPDATE listings.listings SET status = 'unavailable' WHERE owner_user_id = @userId AND status = 'available'",
                ("userId", (object)userId));

            await ExecuteAsync(conn, tx, cancellationToken,
                "UPDATE trades.trade_proposals SET status = 'Rejected', updated_at_utc = NOW() WHERE status = 'Pending' AND (sender_user_id = @userId OR receiver_user_id = @userId)",
                ("userId", (object)userId));

            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task UnsuspendUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            await ExecuteAsync(conn, tx, cancellationToken,
                "UPDATE auth.user_accounts SET is_suspended = false WHERE id = @userId",
                ("userId", (object)userId));

            await ExecuteAsync(conn, tx, cancellationToken,
                "UPDATE listings.listings SET status = 'available' WHERE owner_user_id = @userId AND status = 'unavailable'",
                ("userId", (object)userId));

            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteUserDataAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            await ExecuteAsync(conn, tx, cancellationToken,
                "DELETE FROM listings.listings WHERE owner_user_id = @userId",
                ("userId", (object)userId));

            await ExecuteAsync(conn, tx, cancellationToken,
                "DELETE FROM trades.trade_proposals WHERE sender_user_id = @userId OR receiver_user_id = @userId",
                ("userId", (object)userId));

            await ExecuteAsync(conn, tx, cancellationToken,
                "DELETE FROM auth.user_accounts WHERE id = @userId",
                ("userId", (object)userId));

            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<AdminStatsResponse> GetStatsAsync(CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        await using var cmd = new NpgsqlCommand(@"
            SELECT
                (SELECT COUNT(*) FROM auth.user_accounts)::int AS total_users,
                (SELECT COUNT(*) FROM listings.listings)::int AS total_products,
                (SELECT COUNT(*) FROM trades.trade_proposals)::int AS total_trades,
                (SELECT COUNT(*) FROM trades.trade_proposals WHERE status = 'Accepted')::int AS active_trades,
                (SELECT COUNT(*) FROM trades.trade_proposals WHERE status = 'Completed')::int AS completed_trades,
                (SELECT COUNT(*) FROM trades.trade_proposals WHERE status = 'Pending')::int AS pending_trades,
                (SELECT COUNT(*) FROM reports.reports)::int AS total_reports,
                (SELECT COUNT(*) FROM reports.reports WHERE status = 'pending')::int AS pending_reports;
            ", conn);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        int totalUsers = reader.GetInt32(0);
        int totalProducts = reader.GetInt32(1);
        int totalTrades = reader.GetInt32(2);
        int activeTrades = reader.GetInt32(3);
        int completedTrades = reader.GetInt32(4);
        int pendingTrades = reader.GetInt32(5);
        int totalReports = reader.GetInt32(6);
        int pendingReports = reader.GetInt32(7);
        await reader.CloseAsync();

        await using var roleCmd = new NpgsqlCommand(
            "SELECT role, COUNT(*)::int FROM auth.user_accounts GROUP BY role", conn);
        await using var roleReader = await roleCmd.ExecuteReaderAsync(cancellationToken);
        var byRole = new Dictionary<string, int>();
        while (await roleReader.ReadAsync(cancellationToken))
            byRole[roleReader.GetString(0)] = roleReader.GetInt32(1);

        return new AdminStatsResponse(
            totalUsers, totalProducts, totalTrades,
            activeTrades, completedTrades, pendingTrades,
            byRole, totalReports, pendingReports,
            _timeProvider.GetUtcNow());
    }

    public async Task<ProfileStatsResponse> GetProfileStatsAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        await using var cmd = new NpgsqlCommand(@"
            SELECT
                (SELECT COUNT(*) FROM listings.listings WHERE owner_user_id = @userId)::int,
                (SELECT COUNT(*) FROM trades.trade_proposals
                 WHERE (sender_user_id = @userId OR receiver_user_id = @userId)
                   AND status IN ('Accepted','Completed'))::int,
                (SELECT COUNT(*) FROM reputation.reviews WHERE reviewee_user_id = @userId)::int;
            ", conn);
        cmd.Parameters.AddWithValue("userId", userId);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        return new ProfileStatsResponse(
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetInt32(2));
    }

    public async Task<IReadOnlyList<AdminListingResponse>> GetListingsAsync(
        string? search, string? category, string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var sql = """
            SELECT id, owner_user_id, title, category, status, is_active, asking_price, condition, created_at_utc
            FROM listings.listings
            WHERE (@search   IS NULL OR title ILIKE '%' || @search   || '%')
              AND (@category IS NULL OR UPPER(category) = UPPER(@category))
              AND (@status   IS NULL OR UPPER(status)   = UPPER(@status))
            ORDER BY created_at_utc DESC
            LIMIT @pageSize OFFSET @offset
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.Add(new NpgsqlParameter("search",   NpgsqlDbType.Text) { Value = (object?)search   ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("category", NpgsqlDbType.Text) { Value = (object?)category ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("status",   NpgsqlDbType.Text) { Value = (object?)status   ?? DBNull.Value });
        cmd.Parameters.AddWithValue("pageSize", pageSize);
        cmd.Parameters.AddWithValue("offset",   (page - 1) * pageSize);

        var listings = new List<AdminListingResponse>();
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await r.ReadAsync(cancellationToken))
            listings.Add(new AdminListingResponse(
                r.GetGuid(0), r.GetGuid(1), r.GetString(2), r.GetString(3),
                r.GetString(4), r.GetBoolean(5),
                r.IsDBNull(6) ? null : r.GetDecimal(6),
                r.IsDBNull(7) ? null : r.GetString(7),
                r.GetFieldValue<DateTimeOffset>(8)));
        return listings;
    }

    public async Task SoftDeleteListingAsync(Guid listingId, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(
            "UPDATE listings.listings SET is_active = false WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("id", listingId);
        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        if (rows == 0) throw new AdminValidationException("Listing not found.");
    }

    public async Task UpdateListingStatusAsync(Guid listingId, string status, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(
            "UPDATE listings.listings SET status = @status WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("status", status);
        cmd.Parameters.AddWithValue("id", listingId);
        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        if (rows == 0) throw new AdminValidationException("Listing not found.");
    }

    private static AdminUserResponse ReadUser(NpgsqlDataReader r) =>
        new(r.GetGuid(0),
            r.GetString(1),
            r.GetString(2),
            r.GetString(3),
            r.GetString(4),
            r.IsDBNull(5) ? null : r.GetString(5),
            r.GetBoolean(6),
            r.GetBoolean(7),
            r.GetDecimal(8),
            r.GetFieldValue<DateTimeOffset>(9));

    private static async Task ExecuteAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        CancellationToken ct,
        string sql,
        params (string name, object value)[] parameters)
    {
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
