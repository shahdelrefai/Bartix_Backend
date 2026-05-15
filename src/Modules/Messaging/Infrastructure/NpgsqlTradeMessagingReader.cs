using Bartrix.Modules.Messaging.Application;
using Npgsql;

namespace Bartrix.Modules.Messaging.Infrastructure;

public sealed class NpgsqlTradeMessagingReader : ITradeMessagingReader
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlTradeMessagingReader(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<TradeConversationAccessSnapshot?> GetTradeAsync(Guid tradeProposalId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, sender_user_id, receiver_user_id
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

        return new TradeConversationAccessSnapshot(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetGuid(2));
    }
}
