using Bartrix.Modules.Trades.Application;
using Npgsql;

namespace Bartrix.Modules.Trades.Infrastructure;

public sealed class NpgsqlListingTradeValidationReader : IListingTradeValidationReader
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlListingTradeValidationReader(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<ListingOwnershipSnapshot?> GetListingAsync(Guid listingId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, owner_user_id, is_active
            FROM listings.listings
            WHERE id = @listingId;
            """;

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("listingId", listingId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ListingOwnershipSnapshot(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetBoolean(2));
    }

    public async Task<IReadOnlyList<ListingOwnershipSnapshot>> GetListingsAsync(IEnumerable<Guid> listingIds, CancellationToken cancellationToken)
    {
        var ids = listingIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return Array.Empty<ListingOwnershipSnapshot>();
        }

        const string sql = """
            SELECT id, owner_user_id, is_active
            FROM listings.listings
            WHERE id = ANY(@listingIds);
            """;

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("listingIds", ids);

        var items = new List<ListingOwnershipSnapshot>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new ListingOwnershipSnapshot(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetBoolean(2)));
        }

        return items;
    }
}
