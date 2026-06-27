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

            -- Parity columns (added idempotently for existing databases).
            ALTER TABLE trades.trade_proposals
                ADD COLUMN IF NOT EXISTS sender_user_name character varying(200) NULL,
                ADD COLUMN IF NOT EXISTS receiver_user_name character varying(200) NULL,
                ADD COLUMN IF NOT EXISTS type character varying(40) NOT NULL DEFAULT 'any',
                ADD COLUMN IF NOT EXISTS rejection_reason character varying(500) NULL,
                ADD COLUMN IF NOT EXISTS is_counter_offer boolean NOT NULL DEFAULT false,
                ADD COLUMN IF NOT EXISTS parent_trade_id uuid NULL,
                ADD COLUMN IF NOT EXISTS is_from_premium boolean NOT NULL DEFAULT false,
                ADD COLUMN IF NOT EXISTS requested_listing_ids uuid[] NOT NULL DEFAULT '{}',
                ADD COLUMN IF NOT EXISTS delivery_provided_by uuid[] NOT NULL DEFAULT '{}',
                ADD COLUMN IF NOT EXISTS expires_at_utc timestamp with time zone NOT NULL DEFAULT (now() + interval '7 days');

            CREATE INDEX IF NOT EXISTS ix_trades_trade_proposals_parent_trade_id
                ON trades.trade_proposals (parent_trade_id);

            CREATE TABLE IF NOT EXISTS trades.trade_proposal_offered_listings (
                trade_proposal_id uuid NOT NULL,
                listing_id uuid NOT NULL,
                PRIMARY KEY (trade_proposal_id, listing_id),
                CONSTRAINT fk_trades_trade_proposal_offered_listings_trade_proposals
                    FOREIGN KEY (trade_proposal_id) REFERENCES trades.trade_proposals (id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS trades.trade_counter_offers (
                id uuid PRIMARY KEY,
                trade_proposal_id uuid NOT NULL,
                from_user_id uuid NOT NULL,
                to_user_id uuid NOT NULL,
                offered_listing_ids uuid[] NOT NULL DEFAULT '{}',
                requested_listing_ids uuid[] NOT NULL DEFAULT '{}',
                message character varying(1000) NULL,
                is_accepted boolean NOT NULL DEFAULT false,
                created_at_utc timestamp with time zone NOT NULL,
                CONSTRAINT fk_trades_trade_counter_offers_trade_proposals
                    FOREIGN KEY (trade_proposal_id) REFERENCES trades.trade_proposals (id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS ix_trades_trade_counter_offers_trade_proposal_id
                ON trades.trade_counter_offers (trade_proposal_id);

            CREATE TABLE IF NOT EXISTS trades.trade_history (
                id uuid PRIMARY KEY,
                trade_proposal_id uuid NOT NULL,
                action character varying(64) NOT NULL,
                performed_by_user_id uuid NOT NULL,
                performed_by_user_name character varying(200) NULL,
                details character varying(2000) NULL,
                timestamp_utc timestamp with time zone NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_trades_trade_history_trade_proposal_id
                ON trades.trade_history (trade_proposal_id);
            """;

        await using var command = _dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
