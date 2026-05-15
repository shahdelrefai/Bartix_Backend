using Bartrix.BuildingBlocks.Persistence;
using Npgsql;

namespace Bartrix.Modules.Services.Infrastructure;

public sealed class ServicesDatabaseInitializer : IDatabaseInitializer
{
    private readonly NpgsqlDataSource _dataSource;

    public ServicesDatabaseInitializer(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            CREATE SCHEMA IF NOT EXISTS services;

            CREATE TABLE IF NOT EXISTS services.service_offers (
                id uuid PRIMARY KEY,
                owner_user_id uuid NOT NULL,
                title character varying(200) NOT NULL,
                description character varying(2000) NOT NULL,
                category character varying(100) NOT NULL,
                location character varying(200) NOT NULL,
                fulfillment_mode character varying(20) NOT NULL,
                pricing_type character varying(20) NOT NULL,
                price_amount numeric(12,2) NULL,
                is_available_for_trade boolean NOT NULL,
                is_active boolean NOT NULL,
                created_at_utc timestamp with time zone NOT NULL,
                updated_at_utc timestamp with time zone NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_service_offers_owner_user_id
                ON services.service_offers (owner_user_id);

            CREATE INDEX IF NOT EXISTS ix_service_offers_category
                ON services.service_offers (category);

            CREATE INDEX IF NOT EXISTS ix_service_offers_is_active
                ON services.service_offers (is_active);
            """;

        await using var command = _dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
