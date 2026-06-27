using Bartrix.BuildingBlocks.Persistence;
using Npgsql;

namespace Bartrix.Modules.Messaging.Infrastructure;

public sealed class MessagingDatabaseInitializer : IDatabaseInitializer
{
    private readonly NpgsqlDataSource _dataSource;

    public MessagingDatabaseInitializer(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            CREATE SCHEMA IF NOT EXISTS messaging;

            CREATE TABLE IF NOT EXISTS messaging.conversations (
                id uuid PRIMARY KEY,
                trade_proposal_id uuid NOT NULL,
                participant_a_user_id uuid NOT NULL,
                participant_b_user_id uuid NOT NULL,
                created_at_utc timestamp with time zone NOT NULL,
                updated_at_utc timestamp with time zone NOT NULL
            );

            -- Direct (non-trade) conversations have a null trade_proposal_id.
            ALTER TABLE messaging.conversations
                ALTER COLUMN trade_proposal_id DROP NOT NULL,
                ADD COLUMN IF NOT EXISTS unread_count_a integer NOT NULL DEFAULT 0,
                ADD COLUMN IF NOT EXISTS unread_count_b integer NOT NULL DEFAULT 0;

            DROP INDEX IF EXISTS messaging.ix_messaging_conversations_trade_proposal_id;
            CREATE UNIQUE INDEX IF NOT EXISTS ix_messaging_conversations_trade_proposal_id
                ON messaging.conversations (trade_proposal_id)
                WHERE trade_proposal_id IS NOT NULL;

            CREATE INDEX IF NOT EXISTS ix_messaging_conversations_participant_a_user_id
                ON messaging.conversations (participant_a_user_id);

            CREATE INDEX IF NOT EXISTS ix_messaging_conversations_participant_b_user_id
                ON messaging.conversations (participant_b_user_id);

            CREATE INDEX IF NOT EXISTS ix_messaging_conversations_participants
                ON messaging.conversations (participant_a_user_id, participant_b_user_id);

            CREATE TABLE IF NOT EXISTS messaging.conversation_messages (
                id uuid PRIMARY KEY,
                conversation_id uuid NOT NULL,
                sender_user_id uuid NOT NULL,
                body character varying(2000) NULL,
                created_at_utc timestamp with time zone NOT NULL,
                CONSTRAINT fk_messaging_conversation_messages_conversations
                    FOREIGN KEY (conversation_id) REFERENCES messaging.conversations (id) ON DELETE CASCADE
            );

            ALTER TABLE messaging.conversation_messages
                ALTER COLUMN body DROP NOT NULL,
                ADD COLUMN IF NOT EXISTS image_url character varying(500) NULL,
                ADD COLUMN IF NOT EXISTS is_read boolean NOT NULL DEFAULT false;

            CREATE INDEX IF NOT EXISTS ix_messaging_conversation_messages_conversation_id
                ON messaging.conversation_messages (conversation_id);
            """;

        await using var command = _dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
