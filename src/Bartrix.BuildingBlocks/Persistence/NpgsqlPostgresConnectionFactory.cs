using Npgsql;

namespace Bartrix.BuildingBlocks.Persistence;

public sealed class NpgsqlPostgresConnectionFactory : IPostgresConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlPostgresConnectionFactory(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public ValueTask<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        return _dataSource.OpenConnectionAsync(cancellationToken);
    }
}
