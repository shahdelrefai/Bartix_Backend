using Bartrix.BuildingBlocks.Persistence;
using Npgsql;

namespace Bartrix.Modules.Listings.Infrastructure;

public sealed class ListingsDatabaseInitializer : IDatabaseInitializer
{
    private readonly NpgsqlDataSource _dataSource;

    public ListingsDatabaseInitializer(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            CREATE SCHEMA IF NOT EXISTS listings;

            CREATE TABLE IF NOT EXISTS listings.listings (
                id uuid PRIMARY KEY,
                owner_user_id uuid NOT NULL,
                title character varying(200) NOT NULL,
                description character varying(2000) NOT NULL,
                category character varying(100) NOT NULL,
                condition character varying(20) NOT NULL,
                location character varying(200) NOT NULL,
                asking_price numeric(12,2) NULL,
                is_available_for_swap boolean NOT NULL,
                is_available_for_sale boolean NOT NULL,
                is_active boolean NOT NULL,
                created_at_utc timestamp with time zone NOT NULL,
                updated_at_utc timestamp with time zone NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_listings_owner_user_id
                ON listings.listings (owner_user_id);

            CREATE INDEX IF NOT EXISTS ix_listings_category
                ON listings.listings (category);

            CREATE INDEX IF NOT EXISTS ix_listings_is_active
                ON listings.listings (is_active);

            -- Product-parity columns (added idempotently for existing databases).
            ALTER TABLE listings.listings
                ADD COLUMN IF NOT EXISTS owner_name character varying(200) NULL,
                ADD COLUMN IF NOT EXISTS type character varying(20) NOT NULL DEFAULT 'item',
                ADD COLUMN IF NOT EXISTS status character varying(20) NOT NULL DEFAULT 'available',
                ADD COLUMN IF NOT EXISTS transaction_type character varying(20) NOT NULL DEFAULT 'barter',
                ADD COLUMN IF NOT EXISTS price numeric(12,2) NULL,
                ADD COLUMN IF NOT EXISTS desired_swap_category character varying(100) NULL,
                ADD COLUMN IF NOT EXISTS custom_category character varying(100) NULL,
                ADD COLUMN IF NOT EXISTS latitude double precision NULL,
                ADD COLUMN IF NOT EXISTS longitude double precision NULL,
                ADD COLUMN IF NOT EXISTS tags text[] NOT NULL DEFAULT '{}',
                ADD COLUMN IF NOT EXISTS view_count integer NOT NULL DEFAULT 0,
                ADD COLUMN IF NOT EXISTS is_owner_premium boolean NOT NULL DEFAULT false,
                ADD COLUMN IF NOT EXISTS service_category character varying(100) NULL,
                ADD COLUMN IF NOT EXISTS custom_service_category character varying(100) NULL,
                ADD COLUMN IF NOT EXISTS estimated_duration integer NULL,
                ADD COLUMN IF NOT EXISTS price_range numeric(12,2) NULL,
                ADD COLUMN IF NOT EXISTS availability_schedule character varying(200) NULL,
                ADD COLUMN IF NOT EXISTS skills text[] NOT NULL DEFAULT '{}';

            CREATE INDEX IF NOT EXISTS ix_listings_status
                ON listings.listings (status);

            CREATE TABLE IF NOT EXISTS listings.listing_images (
                id uuid PRIMARY KEY,
                listing_id uuid NOT NULL,
                url character varying(500) NOT NULL,
                sort_order integer NOT NULL,
                CONSTRAINT fk_listings_listing_images_listings
                    FOREIGN KEY (listing_id) REFERENCES listings.listings (id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS listings.listing_favorites (
                user_id uuid NOT NULL,
                listing_id uuid NOT NULL,
                created_at_utc timestamp with time zone NOT NULL,
                PRIMARY KEY (user_id, listing_id)
            );

            CREATE INDEX IF NOT EXISTS ix_listings_listing_favorites_listing_id
                ON listings.listing_favorites (listing_id);

            CREATE TABLE IF NOT EXISTS listings.listing_views (
                listing_id uuid NOT NULL,
                user_id uuid NOT NULL,
                created_at_utc timestamp with time zone NOT NULL,
                PRIMARY KEY (listing_id, user_id)
            );

            CREATE TABLE IF NOT EXISTS listings.listing_reports (
                listing_id uuid NOT NULL,
                user_id uuid NOT NULL,
                created_at_utc timestamp with time zone NOT NULL,
                PRIMARY KEY (listing_id, user_id)
            );
            """;

        await using var command = _dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
