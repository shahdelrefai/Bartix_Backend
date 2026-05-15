using Bartrix.BuildingBlocks.Persistence;
using Npgsql;

namespace Bartrix.Modules.Trades.Infrastructure;

public sealed class TradesDatabaseInitializer : IDatabaseInitializer
{
    private readonly NpgsqlDataSource _dataSource;

    public TradesDatabaseInitializer(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            CREATE SCHEMA IF NOT EXISTS trades;

            CREATE TABLE IF NOT EXISTS trades.trade_proposals (
                id uuid PRIMARY KEY,
                sender_user_id uuid NOT NULL,
                receiver_user_id uuid NOT NULL,
                requested_listing_id uuid NOT NULL,
                message character varying(1000) NULL,
                status character varying(20) NOT NULL,
                created_at_utc timestamp with time zone NOT NULL,
                updated_at_utc timestamp with time zone NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_trades_trade_proposals_sender_user_id
                ON trades.trade_proposals (sender_user_id);

            CREATE INDEX IF NOT EXISTS ix_trades_trade_proposals_receiver_user_id
                ON trades.trade_proposals (receiver_user_id);

            CREATE INDEX IF NOT EXISTS ix_trades_trade_proposals_status
                ON trades.trade_proposals (status);

            CREATE TABLE IF NOT EXISTS trades.trade_proposal_offered_listings (
                trade_proposal_id uuid NOT NULL,
                listing_id uuid NOT NULL,
                PRIMARY KEY (trade_proposal_id, listing_id),
                CONSTRAINT fk_trades_trade_proposal_offered_listings_trade_proposals
                    FOREIGN KEY (trade_proposal_id) REFERENCES trades.trade_proposals (id) ON DELETE CASCADE
            );
            """;

        await using var command = _dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
