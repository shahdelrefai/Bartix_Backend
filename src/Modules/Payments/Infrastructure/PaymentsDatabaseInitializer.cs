using Bartrix.BuildingBlocks.Persistence;
using Npgsql;

namespace Bartrix.Modules.Payments.Infrastructure;

public sealed class PaymentsDatabaseInitializer(NpgsqlDataSource dataSource) : IDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            CREATE SCHEMA IF NOT EXISTS payments;

            CREATE TABLE IF NOT EXISTS payments.payments (
                id uuid PRIMARY KEY,
                buyer_id uuid NOT NULL,
                seller_id uuid NOT NULL,
                product_title character varying(500) NOT NULL,
                amount numeric(14,2) NOT NULL,
                status character varying(20) NOT NULL DEFAULT 'pending',
                paymob_transaction_id character varying(200) NULL,
                is_credited boolean NOT NULL DEFAULT false,
                created_at_utc timestamp with time zone NOT NULL,
                updated_at_utc timestamp with time zone NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_payments_payments_buyer_id
                ON payments.payments (buyer_id);

            CREATE INDEX IF NOT EXISTS ix_payments_payments_seller_id
                ON payments.payments (seller_id);

            CREATE INDEX IF NOT EXISTS ix_payments_payments_paymob_transaction_id
                ON payments.payments (paymob_transaction_id)
                WHERE paymob_transaction_id IS NOT NULL;

            ALTER TABLE payments.payments ADD COLUMN IF NOT EXISTS fee_amount numeric(14,2) NOT NULL DEFAULT 0;
            """;

        await using var cmd = dataSource.CreateCommand(sql);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
