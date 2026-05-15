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

            CREATE UNIQUE INDEX IF NOT EXISTS ix_reputation_reviews_trade_reviewer
                ON reputation.reputation_reviews (trade_proposal_id, reviewer_user_id);

            CREATE INDEX IF NOT EXISTS ix_reputation_reviews_reviewee_user_id
                ON reputation.reputation_reviews (reviewee_user_id);
            """;

        await using var command = _dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
