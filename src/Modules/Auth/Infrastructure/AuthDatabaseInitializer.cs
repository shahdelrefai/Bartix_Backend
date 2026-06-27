using Bartrix.BuildingBlocks.Persistence;
using Npgsql;

namespace Bartrix.Modules.Auth.Infrastructure;

public sealed class AuthDatabaseInitializer : IDatabaseInitializer
{
    private readonly NpgsqlDataSource _dataSource;

    public AuthDatabaseInitializer(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            CREATE SCHEMA IF NOT EXISTS auth;

            CREATE TABLE IF NOT EXISTS auth.user_accounts (
                id uuid PRIMARY KEY,
                email character varying(256) NOT NULL,
                normalized_email character varying(256) NOT NULL,
                password_hash character varying(512) NOT NULL,
                display_name character varying(200) NOT NULL,
                phone_number character varying(32) NULL,
                normalized_phone_number character varying(32) NULL,
                is_phone_verified boolean NOT NULL,
                created_at_utc timestamp with time zone NOT NULL,
                last_login_at_utc timestamp with time zone NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ix_auth_user_accounts_normalized_email
                ON auth.user_accounts (normalized_email);

            CREATE UNIQUE INDEX IF NOT EXISTS ix_auth_user_accounts_normalized_phone_number
                ON auth.user_accounts (normalized_phone_number)
                WHERE normalized_phone_number IS NOT NULL;

            CREATE TABLE IF NOT EXISTS auth.refresh_token_sessions (
                id uuid PRIMARY KEY,
                user_account_id uuid NOT NULL,
                token_hash character varying(128) NOT NULL,
                created_at_utc timestamp with time zone NOT NULL,
                expires_at_utc timestamp with time zone NOT NULL,
                revoked_at_utc timestamp with time zone NULL,
                CONSTRAINT fk_auth_refresh_token_sessions_user_accounts
                    FOREIGN KEY (user_account_id) REFERENCES auth.user_accounts (id) ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ix_auth_refresh_token_sessions_token_hash
                ON auth.refresh_token_sessions (token_hash);

            CREATE TABLE IF NOT EXISTS auth.phone_otp_challenges (
                id uuid PRIMARY KEY,
                phone_number character varying(32) NOT NULL,
                normalized_phone_number character varying(32) NOT NULL,
                purpose character varying(64) NOT NULL,
                code_hash character varying(512) NOT NULL,
                provider_challenge_id character varying(128) NULL,
                max_attempts integer NOT NULL,
                attempt_count integer NOT NULL,
                is_consumed boolean NOT NULL,
                created_at_utc timestamp with time zone NOT NULL,
                expires_at_utc timestamp with time zone NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_auth_phone_otp_challenges_phone_purpose
                ON auth.phone_otp_challenges (normalized_phone_number, purpose);

            ALTER TABLE auth.user_accounts ADD COLUMN IF NOT EXISTS role character varying(50) NOT NULL DEFAULT 'user';
            ALTER TABLE auth.user_accounts ADD COLUMN IF NOT EXISTS is_suspended boolean NOT NULL DEFAULT false;
            ALTER TABLE auth.user_accounts ADD COLUMN IF NOT EXISTS profile_image_url character varying(500) NULL;
            ALTER TABLE auth.user_accounts ADD COLUMN IF NOT EXISTS is_premium_active boolean NOT NULL DEFAULT false;
            ALTER TABLE auth.user_accounts ADD COLUMN IF NOT EXISTS premium_expires_at_utc timestamp with time zone NULL;
            ALTER TABLE auth.user_accounts ADD COLUMN IF NOT EXISTS wallet_balance numeric(12,2) NOT NULL DEFAULT 0;
            ALTER TABLE auth.user_accounts ADD COLUMN IF NOT EXISTS language_code character varying(10) NOT NULL DEFAULT 'en';
            """;

        await using var command = _dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
