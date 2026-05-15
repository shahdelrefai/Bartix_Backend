using Bartrix.Modules.Messaging.Contracts;
using Bartrix.Modules.Messaging.Domain;
using Bartrix.Modules.Messaging.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Messaging.Application;

public sealed class MessagingService : IMessagingService
{
    private readonly MessagingDbContext _dbContext;
    private readonly ITradeMessagingReader _tradeReader;
    private readonly IConversationRealtimeNotifier _notifier;
    private readonly TimeProvider _timeProvider;

    public MessagingService(
        MessagingDbContext dbContext,
        ITradeMessagingReader tradeReader,
        IConversationRealtimeNotifier notifier,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _tradeReader = tradeReader;
        _notifier = notifier;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<ConversationListItemResponse>> GetMyConversationsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var items = await _dbContext.Conversations
            .AsNoTracking()
            .Include(x => x.Messages.OrderByDescending(m => m.CreatedAtUtc).Take(1))
            .Where(x => x.ParticipantAUserId == userId || x.ParticipantBUserId == userId)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

        return items.Select(x =>
        {
            var lastMessage = x.Messages.OrderByDescending(m => m.CreatedAtUtc).FirstOrDefault();
            var otherUserId = x.ParticipantAUserId == userId ? x.ParticipantBUserId : x.ParticipantAUserId;

            return new ConversationListItemResponse(
                x.Id,
                x.TradeProposalId,
                otherUserId,
                x.UpdatedAtUtc,
                lastMessage is null ? null : Map(lastMessage));
        }).ToList();
    }

    public async Task<ConversationResponse> GetTradeConversationAsync(Guid userId, Guid tradeProposalId, CancellationToken cancellationToken)
    {
        var conversation = await GetOrCreateConversationAsync(userId, tradeProposalId, cancellationToken);

        await _dbContext.Entry(conversation)
            .Collection(x => x.Messages)
            .LoadAsync(cancellationToken);

        return Map(conversation);
    }

    public async Task<ConversationMessageResponse> SendTradeMessageAsync(Guid userId, Guid tradeProposalId, SendMessageRequest request, CancellationToken cancellationToken)
    {
        var body = NormalizeMessageBody(request.Body);
        var conversation = await GetOrCreateConversationAsync(userId, tradeProposalId, cancellationToken);
        var message = conversation.AddMessage(userId, body, _timeProvider.GetUtcNow());
        _dbContext.Messages.Add(message);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = Map(message);
        await _notifier.NotifyMessageSentAsync(conversation.Id, response, cancellationToken);
        return response;
    }

    private async Task<Conversation> GetOrCreateConversationAsync(Guid userId, Guid tradeProposalId, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Conversations
            .Include(x => x.Messages)
            .SingleOrDefaultAsync(x => x.TradeProposalId == tradeProposalId, cancellationToken);

        if (existing is not null)
        {
            EnsureParticipant(userId, existing);
            return existing;
        }

        var trade = await _tradeReader.GetTradeAsync(tradeProposalId, cancellationToken);
        if (trade is null)
        {
            throw new MessagingValidationException("Trade proposal was not found.");
        }

        if (trade.SenderUserId != userId && trade.ReceiverUserId != userId)
        {
            throw new MessagingValidationException("You do not have access to this trade conversation.");
        }

        var nowUtc = _timeProvider.GetUtcNow();
        var conversation = new Conversation(
            trade.TradeProposalId,
            trade.SenderUserId,
            trade.ReceiverUserId,
            nowUtc);

        _dbContext.Conversations.Add(conversation);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    private static void EnsureParticipant(Guid userId, Conversation conversation)
    {
        if (!conversation.HasParticipant(userId))
        {
            throw new MessagingValidationException("You do not have access to this conversation.");
        }
    }

    private static string NormalizeMessageBody(string body)
    {
        var normalized = body?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new MessagingValidationException("Message body is required.");
        }

        if (normalized.Length > 2000)
        {
            throw new MessagingValidationException("Message body cannot exceed 2000 characters.");
        }

        return normalized;
    }

    private static ConversationResponse Map(Conversation conversation)
    {
        return new ConversationResponse(
            conversation.Id,
            conversation.TradeProposalId,
            conversation.ParticipantAUserId,
            conversation.ParticipantBUserId,
            conversation.CreatedAtUtc,
            conversation.UpdatedAtUtc,
            conversation.Messages
                .OrderBy(x => x.CreatedAtUtc)
                .Select(Map)
                .ToList());
    }

    private static ConversationMessageResponse Map(ConversationMessage message)
    {
        return new ConversationMessageResponse(
            message.Id,
            message.SenderUserId,
            message.Body,
            message.CreatedAtUtc);
    }
}
