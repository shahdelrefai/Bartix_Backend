using Bartrix.Modules.Notifications.Application;
using Npgsql;

namespace Bartrix.Modules.Notifications.Infrastructure;

public sealed class NpgsqlUserLanguageReader : IUserLanguageReader
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlUserLanguageReader(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<string> GetLanguageCodeAsync(Guid userId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT language_code
            FROM auth.user_accounts
            WHERE id = @userId;
            """;

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("userId", userId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result as string ?? "en";
    }
}
