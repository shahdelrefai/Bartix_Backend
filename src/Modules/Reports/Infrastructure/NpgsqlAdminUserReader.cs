using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Reports.Application;
using Npgsql;

namespace Bartrix.Modules.Reports.Infrastructure;

public sealed class NpgsqlAdminUserReader : IAdminUserReader
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public NpgsqlAdminUserReader(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Guid>> GetAdminUserIdsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(
            "SELECT id FROM auth.user_accounts WHERE role = 'admin'", connection);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var ids = new List<Guid>();
        while (await reader.ReadAsync(cancellationToken))
            ids.Add(reader.GetGuid(0));

        return ids;
    }
}
