using Bartrix.BuildingBlocks.Persistence;
using Npgsql;

namespace Bartrix.Modules.Reports.Infrastructure;

public sealed class ReportsDatabaseInitializer : IDatabaseInitializer
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public ReportsDatabaseInitializer(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        await using var cmd = new NpgsqlCommand(@"
            CREATE SCHEMA IF NOT EXISTS reports;

            CREATE TABLE IF NOT EXISTS reports.reports (
                id uuid PRIMARY KEY,
                reporter_id uuid NOT NULL,
                reporter_name varchar(200) NOT NULL,
                reported_product_id uuid NOT NULL,
                reported_product_title varchar(200) NOT NULL,
                reported_product_owner_id uuid NOT NULL,
                reason varchar(50) NOT NULL,
                description text NOT NULL DEFAULT '',
                status varchar(20) NOT NULL DEFAULT 'pending',
                created_at_utc timestamptz NOT NULL,
                reviewed_at_utc timestamptz,
                admin_note text
            );

            CREATE INDEX IF NOT EXISTS ix_reports_status ON reports.reports (status);
            CREATE INDEX IF NOT EXISTS ix_reports_reporter_id ON reports.reports (reporter_id);
            CREATE INDEX IF NOT EXISTS ix_reports_product_id ON reports.reports (reported_product_id);
        ", connection);

        await cmd.ExecuteNonQueryAsync();
    }
}
