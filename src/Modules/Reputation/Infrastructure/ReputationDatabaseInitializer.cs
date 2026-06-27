using Bartrix.BuildingBlocks.Persistence;
using Npgsql;

namespace Bartrix.Modules.Reputation.Infrastructure;

public sealed class ReputationDatabaseInitializer : IDatabaseInitializer
{
    private readonly NpgsqlDataSource _dataSource;

    public ReputationDatabaseInitializer(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            CREATE SCHEMA IF NOT EXISTS reputation;

            CREATE TABLE IF NOT EXISTS reputation.reputation_reviews (
                id uuid PRIMARY KEY,
                trade_proposal_id uuid NOT NULL,
                reviewer_user_id uuid NOT NULL,
                reviewee_user_id uuid NOT NULL,
                rating integer NOT NULL,
                comment character varying(1000) NULL,
                created_at_utc timestamp with time zone NOT NULL
            );

            -- Direct user-to-user reviews have a null trade_proposal_id.
            ALTER TABLE reputation.reputation_reviews
                ALTER COLUMN trade_proposal_id DROP NOT NULL,
                ADD COLUMN IF NOT EXISTS reviewer_name character varying(200) NULL,
                ADD COLUMN IF NOT EXISTS title character varying(200) NULL;

            DROP INDEX IF EXISTS reputation.ix_reputation_reviews_trade_reviewer;
            CREATE UNIQUE INDEX IF NOT EXISTS ix_reputation_reviews_trade_reviewer
                ON reputation.reputation_reviews (trade_proposal_id, reviewer_user_id)
                WHERE trade_proposal_id IS NOT NULL;

            CREATE INDEX IF NOT EXISTS ix_reputation_reviews_reviewee_user_id
                ON reputation.reputation_reviews (reviewee_user_id);
            """;

        await using var command = _dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
