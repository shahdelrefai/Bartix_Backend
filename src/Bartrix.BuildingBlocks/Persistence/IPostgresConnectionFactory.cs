using Npgsql;

namespace Bartrix.BuildingBlocks.Persistence;

public interface IPostgresConnectionFactory
{
    ValueTask<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}
