using Bartrix.BuildingBlocks.Persistence;
using Npgsql;

namespace Bartrix.Modules.Notifications.Infrastructure;

public sealed class NotificationsDatabaseInitializer : IDatabaseInitializer
{
    private readonly NpgsqlDataSource _dataSource;

    public NotificationsDatabaseInitializer(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            CREATE SCHEMA IF NOT EXISTS notifications;

            CREATE TABLE IF NOT EXISTS notifications.notifications (
                id uuid PRIMARY KEY,
                user_id uuid NOT NULL,
                title character varying(200) NOT NULL,
                body character varying(1000) NOT NULL,
                type character varying(40) NOT NULL,
                related_id character varying(100) NULL,
                is_read boolean NOT NULL DEFAULT false,
                created_at_utc timestamp with time zone NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_notifications_user_unread
                ON notifications.notifications (user_id, is_read);

            CREATE INDEX IF NOT EXISTS ix_notifications_user_created
                ON notifications.notifications (user_id, created_at_utc);
            """;

        await using var command = _dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
