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

            CREATE TABLE IF NOT EXISTS listings.listing_images (
                id uuid PRIMARY KEY,
                listing_id uuid NOT NULL,
                url character varying(500) NOT NULL,
                sort_order integer NOT NULL,
                CONSTRAINT fk_listings_listing_images_listings
                    FOREIGN KEY (listing_id) REFERENCES listings.listings (id) ON DELETE CASCADE
            );
            """;

        await using var command = _dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
