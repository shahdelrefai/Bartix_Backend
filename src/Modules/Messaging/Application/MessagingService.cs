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

        return items.Select(x => MapListItem(x, userId)).ToList();
    }

    public async Task<ConversationResponse> GetTradeConversationAsync(Guid userId, Guid tradeProposalId, CancellationToken cancellationToken)
    {
        var conversation = await GetOrCreateTradeConversationAsync(userId, tradeProposalId, cancellationToken);

        await _dbContext.Entry(conversation)
            .Collection(x => x.Messages)
            .LoadAsync(cancellationToken);

        return Map(conversation, userId);
    }

    public async Task<ConversationMessageResponse> SendTradeMessageAsync(Guid userId, Guid tradeProposalId, SendMessageRequest request, CancellationToken cancellationToken)
    {
        var conversation = await GetOrCreateTradeConversationAsync(userId, tradeProposalId, cancellationToken);
        return await AppendMessageAsync(conversation, userId, request, cancellationToken);
    }

    public async Task<ConversationResponse> GetOrCreateDirectConversationAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken)
    {
        if (userId == otherUserId)
        {
            throw new MessagingValidationException("You cannot start a conversation with yourself.");
        }

        var (a, b) = Order(userId, otherUserId);

        var conversation = await _dbContext.Conversations
            .Include(x => x.Messages)
            .SingleOrDefaultAsync(x => x.TradeProposalId == null
                && x.ParticipantAUserId == a && x.ParticipantBUserId == b, cancellationToken);

        if (conversation is null)
        {
            conversation = new Conversation(null, a, b, _timeProvider.GetUtcNow());
            _dbContext.Conversations.Add(conversation);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Map(conversation, userId);
    }

    public async Task<ConversationResponse> GetConversationAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken)
    {
        var conversation = await _dbContext.Conversations
            .Include(x => x.Messages)
            .SingleOrDefaultAsync(x => x.Id == conversationId, cancellationToken)
            ?? throw new MessagingValidationException("Conversation was not found.");

        EnsureParticipant(userId, conversation);
        return Map(conversation, userId);
    }

    public async Task<ConversationMessageResponse> SendConversationMessageAsync(Guid userId, Guid conversationId, SendMessageRequest request, CancellationToken cancellationToken)
    {
        var conversation = await _dbContext.Conversations
            .Include(x => x.Messages)
            .SingleOrDefaultAsync(x => x.Id == conversationId, cancellationToken)
            ?? throw new MessagingValidationException("Conversation was not found.");

        EnsureParticipant(userId, conversation);
        return await AppendMessageAsync(conversation, userId, request, cancellationToken);
    }

    public async Task MarkConversationReadAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken)
    {
        var conversation = await _dbContext.Conversations
            .Include(x => x.Messages)
            .SingleOrDefaultAsync(x => x.Id == conversationId, cancellationToken)
            ?? throw new MessagingValidationException("Conversation was not found.");

        EnsureParticipant(userId, conversation);
        conversation.MarkRead(userId);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ConversationMessageResponse> AppendMessageAsync(Conversation conversation, Guid userId, SendMessageRequest request, CancellationToken cancellationToken)
    {
        var (body, imageUrl) = NormalizeMessage(request);
        var message = conversation.AddMessage(userId, body, imageUrl, _timeProvider.GetUtcNow());
        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = Map(message);
        await _notifier.NotifyMessageSentAsync(conversation.Id, response, cancellationToken);

        // Push a conversation-list update (last message + unread badge) to the recipient.
        var recipientId = conversation.OtherParticipant(userId);
        await _notifier.NotifyConversationUpdatedAsync(recipientId, MapListItem(conversation, recipientId), cancellationToken);

        return response;
    }

    private async Task<Conversation> GetOrCreateTradeConversationAsync(Guid userId, Guid tradeProposalId, CancellationToken cancellationToken)
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

    private static (Guid A, Guid B) Order(Guid x, Guid y)
    {
        return x.CompareTo(y) <= 0 ? (x, y) : (y, x);
    }

    private static void EnsureParticipant(Guid userId, Conversation conversation)
    {
        if (!conversation.HasParticipant(userId))
        {
            throw new MessagingValidationException("You do not have access to this conversation.");
        }
    }

    private static (string? Body, string? ImageUrl) NormalizeMessage(SendMessageRequest request)
    {
        var body = string.IsNullOrWhiteSpace(request.Body) ? null : request.Body.Trim();
        var imageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();

        if (body is null && imageUrl is null)
        {
            throw new MessagingValidationException("A message must contain text or an image.");
        }

        if (body is not null && body.Length > 2000)
        {
            throw new MessagingValidationException("Message body cannot exceed 2000 characters.");
        }

        if (imageUrl is not null && imageUrl.Length > 500)
        {
            throw new MessagingValidationException("Image URL cannot exceed 500 characters.");
        }

        return (body, imageUrl);
    }

    private static ConversationListItemResponse MapListItem(Conversation conversation, Guid userId)
    {
        var lastMessage = conversation.Messages.OrderByDescending(m => m.CreatedAtUtc).FirstOrDefault();
        return new ConversationListItemResponse(
            conversation.Id,
            conversation.TradeProposalId,
            conversation.OtherParticipant(userId),
            conversation.UpdatedAtUtc,
            lastMessage is null ? null : Map(lastMessage),
            conversation.UnreadFor(userId));
    }

    private static ConversationResponse Map(Conversation conversation, Guid userId)
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
                .ToList(),
            conversation.UnreadFor(userId));
    }

    private static ConversationMessageResponse Map(ConversationMessage message)
    {
        return new ConversationMessageResponse(
            message.Id,
            message.SenderUserId,
            message.Body,
            message.CreatedAtUtc,
            message.ConversationId,
            message.ImageUrl,
            message.IsRead);
    }
}
