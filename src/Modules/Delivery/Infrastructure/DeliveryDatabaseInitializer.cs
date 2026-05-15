using Bartrix.BuildingBlocks.Persistence;
using Npgsql;

namespace Bartrix.Modules.Delivery.Infrastructure;

public sealed class DeliveryDatabaseInitializer : IDatabaseInitializer
{
    private readonly NpgsqlDataSource _dataSource;

    public DeliveryDatabaseInitializer(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            CREATE SCHEMA IF NOT EXISTS delivery;

            CREATE TABLE IF NOT EXISTS delivery.trade_deliveries (
                id uuid PRIMARY KEY,
                trade_proposal_id uuid NOT NULL,
                participant_a_user_id uuid NOT NULL,
                participant_b_user_id uuid NOT NULL,
                method character varying(30) NULL,
                status character varying(30) NOT NULL,
                location character varying(500) NULL,
                scheduled_at_utc timestamp with time zone NULL,
                notes character varying(1000) NULL,
                created_at_utc timestamp with time zone NOT NULL,
                updated_at_utc timestamp with time zone NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ix_delivery_trade_deliveries_trade_proposal_id
                ON delivery.trade_deliveries (trade_proposal_id);
            """;

        await using var command = _dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
