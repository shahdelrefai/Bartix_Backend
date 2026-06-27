using Bartrix.BuildingBlocks.Persistence;
using Npgsql;

namespace Bartrix.Modules.Wallet.Infrastructure;

public sealed class WalletsDatabaseInitializer(NpgsqlDataSource dataSource) : IDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            CREATE SCHEMA IF NOT EXISTS wallet;

            CREATE TABLE IF NOT EXISTS wallet.transactions (
                id uuid PRIMARY KEY,
                user_id uuid NOT NULL,
                amount numeric(14,2) NOT NULL,
                type character varying(10) NOT NULL,
                reference_id character varying(200) NOT NULL,
                description character varying(500) NOT NULL,
                created_at_utc timestamp with time zone NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_wallet_transactions_user_id
                ON wallet.transactions (user_id);

            CREATE INDEX IF NOT EXISTS ix_wallet_transactions_created_at_utc
                ON wallet.transactions (created_at_utc DESC);
            """;

        await using var cmd = dataSource.CreateCommand(sql);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
