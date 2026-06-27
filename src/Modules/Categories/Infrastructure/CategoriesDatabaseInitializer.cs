using Bartrix.BuildingBlocks.Persistence;
using Npgsql;

namespace Bartrix.Modules.Categories.Infrastructure;

public sealed class CategoriesDatabaseInitializer : IDatabaseInitializer
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public CategoriesDatabaseInitializer(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        await using var cmd = new NpgsqlCommand(@"
            CREATE SCHEMA IF NOT EXISTS categories;

            CREATE TABLE IF NOT EXISTS categories.approved_categories (
                id uuid PRIMARY KEY,
                name varchar(100) NOT NULL,
                added_by uuid NOT NULL,
                added_by_name varchar(200) NOT NULL,
                added_at_utc timestamptz NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_approved_categories_name
                ON categories.approved_categories (lower(name));

            CREATE TABLE IF NOT EXISTS categories.category_suggestions (
                id uuid PRIMARY KEY,
                suggested_name varchar(100) NOT NULL,
                suggested_by uuid NOT NULL,
                suggested_by_name varchar(200) NOT NULL,
                status varchar(20) NOT NULL DEFAULT 'pending',
                created_at_utc timestamptz NOT NULL,
                reviewed_by uuid,
                reviewed_by_name varchar(200),
                reviewed_at_utc timestamptz
            );

            CREATE INDEX IF NOT EXISTS ix_category_suggestions_status
                ON categories.category_suggestions (status);
        ", connection);

        await cmd.ExecuteNonQueryAsync();
    }
}
