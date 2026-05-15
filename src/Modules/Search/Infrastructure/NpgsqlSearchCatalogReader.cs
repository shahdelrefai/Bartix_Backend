using Bartrix.Modules.Search.Application;
using Npgsql;

namespace Bartrix.Modules.Search.Infrastructure;

public sealed class NpgsqlSearchCatalogReader : ISearchCatalogReader
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlSearchCatalogReader(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<SearchCatalogPage> SearchAsync(SearchCatalogRequest request, CancellationToken cancellationToken)
    {
        var sql = BuildSql(request);

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("search", (object?)request.Search ?? DBNull.Value);
        command.Parameters.AddWithValue("category", (object?)request.Category ?? DBNull.Value);
        command.Parameters.AddWithValue("location", (object?)request.Location ?? DBNull.Value);
        command.Parameters.AddWithValue("ownerUserId", (object?)request.OwnerUserId ?? DBNull.Value);
        command.Parameters.AddWithValue("offset", (request.Page - 1) * request.PageSize);
        command.Parameters.AddWithValue("pageSize", request.PageSize);

        var items = new List<SearchCatalogItem>();
        var totalCount = 0;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            totalCount = reader.GetInt32(13);

            items.Add(new SearchCatalogItem(
                reader.GetGuid(0),
                Enum.Parse<SearchSourceType>(reader.GetString(1), true),
                reader.GetGuid(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                reader.GetBoolean(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.IsDBNull(10) ? null : reader.GetBoolean(10),
                reader.IsDBNull(11) ? null : reader.GetString(11),
                reader.IsDBNull(12) ? null : reader.GetString(12),
                reader.GetFieldValue<DateTimeOffset>(14)));
        }

        return new SearchCatalogPage(items, totalCount);
    }

    private static string BuildSql(SearchCatalogRequest request)
    {
        var includeListings = request.SourceType is SearchSourceType.All or SearchSourceType.Listings;
        var includeServices = request.SourceType is SearchSourceType.All or SearchSourceType.Services;

        var parts = new List<string>();

        if (includeListings)
        {
            parts.Add("""
                SELECT
                    l.id,
                    'Listings' AS type,
                    l.owner_user_id,
                    l.title,
                    l.description,
                    l.category,
                    l.location,
                    l.asking_price AS price_amount,
                    l.is_available_for_swap AS is_available_for_trade,
                    l.condition AS listing_condition,
                    l.is_available_for_sale,
                    NULL::character varying AS fulfillment_mode,
                    NULL::character varying AS pricing_type,
                    l.created_at_utc
                FROM listings.listings l
                WHERE l.is_active = TRUE
                  AND (@ownerUserId IS NULL OR l.owner_user_id = @ownerUserId)
                  AND (@category IS NULL OR UPPER(l.category) = UPPER(@category))
                  AND (@location IS NULL OR UPPER(l.location) LIKE '%' || UPPER(@location) || '%')
                  AND (
                        @search IS NULL OR
                        UPPER(l.title) LIKE '%' || UPPER(@search) || '%' OR
                        UPPER(l.description) LIKE '%' || UPPER(@search) || '%'
                  )
                """);
        }

        if (includeServices)
        {
            parts.Add("""
                SELECT
                    s.id,
                    'Services' AS type,
                    s.owner_user_id,
                    s.title,
                    s.description,
                    s.category,
                    s.location,
                    s.price_amount,
                    s.is_available_for_trade,
                    NULL::character varying AS listing_condition,
                    NULL::boolean AS is_available_for_sale,
                    s.fulfillment_mode,
                    s.pricing_type,
                    s.created_at_utc
                FROM services.service_offers s
                WHERE s.is_active = TRUE
                  AND (@ownerUserId IS NULL OR s.owner_user_id = @ownerUserId)
                  AND (@category IS NULL OR UPPER(s.category) = UPPER(@category))
                  AND (@location IS NULL OR UPPER(s.location) LIKE '%' || UPPER(@location) || '%')
                  AND (
                        @search IS NULL OR
                        UPPER(s.title) LIKE '%' || UPPER(@search) || '%' OR
                        UPPER(s.description) LIKE '%' || UPPER(@search) || '%'
                  )
                """);
        }

        return $"""
            WITH search_results AS (
                {string.Join("\nUNION ALL\n", parts)}
            )
            SELECT
                id,
                type,
                owner_user_id,
                title,
                description,
                category,
                location,
                price_amount,
                is_available_for_trade,
                listing_condition,
                is_available_for_sale,
                fulfillment_mode,
                pricing_type,
                COUNT(*) OVER() AS total_count,
                created_at_utc
            FROM search_results
            ORDER BY created_at_utc DESC
            OFFSET @offset
            LIMIT @pageSize;
            """;
    }
}
