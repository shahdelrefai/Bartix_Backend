using Bartrix.BuildingBlocks.Persistence;
using Npgsql;

namespace Bartrix.Modules.Profiles.Infrastructure;

public sealed class ProfilesDatabaseInitializer : IDatabaseInitializer
{
    private readonly NpgsqlDataSource _dataSource;

    public ProfilesDatabaseInitializer(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            CREATE SCHEMA IF NOT EXISTS profiles;

            CREATE TABLE IF NOT EXISTS profiles.user_profiles (
                user_id uuid PRIMARY KEY,
                display_name character varying(200) NOT NULL,
                bio character varying(1000) NULL,
                location character varying(200) NULL,
                avatar_url character varying(500) NULL,
                created_at_utc timestamp with time zone NOT NULL,
                updated_at_utc timestamp with time zone NOT NULL
            );
            """;

        await using var command = _dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
