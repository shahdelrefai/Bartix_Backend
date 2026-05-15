using Bartrix.Modules.Delivery.Application;
using Npgsql;

namespace Bartrix.Modules.Delivery.Infrastructure;

public sealed class NpgsqlTradeDeliveryReader : ITradeDeliveryReader
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlTradeDeliveryReader(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<TradeDeliveryAccessSnapshot?> GetTradeAsync(Guid tradeProposalId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, sender_user_id, receiver_user_id, status
            FROM trades.trade_proposals
            WHERE id = @tradeProposalId;
            """;

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("tradeProposalId", tradeProposalId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new TradeDeliveryAccessSnapshot(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetGuid(2),
            reader.GetString(3));
    }
}
