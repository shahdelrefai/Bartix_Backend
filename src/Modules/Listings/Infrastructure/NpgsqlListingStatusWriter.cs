using Bartrix.BuildingBlocks.Listings;
using Npgsql;

namespace Bartrix.Modules.Listings.Infrastructure;

internal sealed class NpgsqlListingStatusWriter(NpgsqlDataSource dataSource) : IListingStatusWriter
{
    public async Task SetStatusAsync(Guid listingId, string status, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE listings.listings SET status = @status, updated_at_utc = now() WHERE id = @id;";
        await using var cmd = dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("id", listingId);
        cmd.Parameters.AddWithValue("status", status);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SetManyStatusAsync(IReadOnlyList<Guid> listingIds, string status, CancellationToken cancellationToken)
    {
        if (listingIds.Count == 0) return;
        const string sql = "UPDATE listings.listings SET status = @status, updated_at_utc = now() WHERE id = ANY(@ids);";
        await using var cmd = dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("ids", listingIds.ToArray());
        cmd.Parameters.AddWithValue("status", status);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
