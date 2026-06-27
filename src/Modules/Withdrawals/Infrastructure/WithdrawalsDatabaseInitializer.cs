using Bartrix.BuildingBlocks.Persistence;
using Npgsql;

namespace Bartrix.Modules.Withdrawals.Infrastructure;

public sealed class WithdrawalsDatabaseInitializer(NpgsqlDataSource dataSource) : IDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            CREATE SCHEMA IF NOT EXISTS withdrawals;

            CREATE TABLE IF NOT EXISTS withdrawals.withdrawal_requests (
                id uuid PRIMARY KEY,
                seller_id uuid NOT NULL,
                amount numeric(14,2) NOT NULL,
                status character varying(20) NOT NULL DEFAULT 'pending',
                bank_details character varying(2000) NULL,
                admin_note character varying(500) NULL,
                created_at_utc timestamp with time zone NOT NULL,
                updated_at_utc timestamp with time zone NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_withdrawals_withdrawal_requests_seller_id
                ON withdrawals.withdrawal_requests (seller_id);

            CREATE INDEX IF NOT EXISTS ix_withdrawals_withdrawal_requests_status
                ON withdrawals.withdrawal_requests (status);
            """;

        await using var cmd = dataSource.CreateCommand(sql);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
