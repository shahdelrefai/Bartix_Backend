using Bartrix.Modules.Messaging.Application;
using Bartrix.Modules.Messaging.Contracts;
using Bartrix.Modules.Messaging.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Messaging.Tests;

public sealed class MessagingServiceTests
{
    [Fact]
    public async Task GetTradeConversationAsync_CreatesConversation_ForParticipant()
    {
        await using var dbContext = CreateDbContext();
        var tradeId = Guid.NewGuid();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var service = CreateService(
            dbContext,
            new FakeTradeReader(new TradeConversationAccessSnapshot(tradeId, userA, userB)),
            new RecordingNotifier());

        var response = await service.GetTradeConversationAsync(userA, tradeId, CancellationToken.None);

        Assert.Equal(tradeId, response.TradeProposalId);
        Assert.Empty(response.Messages);
        Assert.True(await dbContext.Conversations.AnyAsync(x => x.TradeProposalId == tradeId));
    }

    [Fact]
    public async Task SendTradeMessageAsync_PersistsMessage_AndNotifies()
    {
        await using var dbContext = CreateDbContext();
        var tradeId = Guid.NewGuid();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var notifier = new RecordingNotifier();
        var service = CreateService(
            dbContext,
            new FakeTradeReader(new TradeConversationAccessSnapshot(tradeId, userA, userB)),
            notifier);

        var message = await service.SendTradeMessageAsync(
            userA,
            tradeId,
            new SendMessageRequest("Hello there"),
            CancellationToken.None);

        Assert.Equal("Hello there", message.Body);
        Assert.Equal(userA, message.SenderUserId);
        Assert.Single(notifier.Messages);
    }

    [Fact]
    public async Task SendTradeMessageAsync_RejectsNonParticipant()
    {
        await using var dbContext = CreateDbContext();
        var tradeId = Guid.NewGuid();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var outsider = Guid.NewGuid();
        var service = CreateService(
            dbContext,
            new FakeTradeReader(new TradeConversationAccessSnapshot(tradeId, userA, userB)),
            new RecordingNotifier());

        await Assert.ThrowsAsync<MessagingValidationException>(() =>
            service.SendTradeMessageAsync(outsider, tradeId, new SendMessageRequest("No access"), CancellationToken.None));
    }

    [Fact]
    public async Task GetMyConversationsAsync_ReturnsOrderedConversations()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var otherA = Guid.NewGuid();
        var otherB = Guid.NewGuid();
        var notifier = new RecordingNotifier();
        var timeProvider = new FixedTimeProvider();
        var tradeA = new FakeTradeReader(new TradeConversationAccessSnapshot(Guid.NewGuid(), userId, otherA));
        var tradeB = new FakeTradeReader(new TradeConversationAccessSnapshot(Guid.NewGuid(), userId, otherB));
        var serviceA = CreateService(
            dbContext,
            tradeA,
            notifier,
            timeProvider);
        var serviceB = CreateService(
            dbContext,
            tradeB,
            notifier,
            timeProvider);

        await serviceA.SendTradeMessageAsync(userId, tradeA.Trade.TradeProposalId, new SendMessageRequest("first"), CancellationToken.None);
        await serviceB.SendTradeMessageAsync(userId, tradeB.Trade.TradeProposalId, new SendMessageRequest("second"), CancellationToken.None);

        var response = await serviceA.GetMyConversationsAsync(userId, CancellationToken.None);

        Assert.Equal(2, response.Count);
        Assert.Equal("second", response[0].LastMessage?.Body);
    }

    [Fact]
    public async Task DirectConversation_IsCreatedOnce_AndReused()
    {
        await using var dbContext = CreateDbContext();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var service = CreateService(dbContext, new FakeTradeReader(new TradeConversationAccessSnapshot(Guid.NewGuid(), userA, userB)), new RecordingNotifier());

        var first = await service.GetOrCreateDirectConversationAsync(userA, userB, CancellationToken.None);
        // Same pair, reversed order, should resolve to the same conversation.
        var second = await service.GetOrCreateDirectConversationAsync(userB, userA, CancellationToken.None);

        Assert.Equal(first.Id, second.Id);
        Assert.Null(first.TradeProposalId);
        Assert.Equal(1, await dbContext.Conversations.CountAsync());
    }

    [Fact]
    public async Task SendConversationMessage_IncrementsRecipientUnread_AndMarkReadClearsIt()
    {
        await using var dbContext = CreateDbContext();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var notifier = new RecordingNotifier();
        var service = CreateService(dbContext, new FakeTradeReader(new TradeConversationAccessSnapshot(Guid.NewGuid(), userA, userB)), notifier);

        var convo = await service.GetOrCreateDirectConversationAsync(userA, userB, CancellationToken.None);
        await service.SendConversationMessageAsync(userA, convo.Id, new SendMessageRequest("hi", null), CancellationToken.None);

        // The recipient (B) should have an unread message and a conversation-update push.
        var bView = await service.GetConversationAsync(userB, convo.Id, CancellationToken.None);
        Assert.Equal(1, bView.UnreadCount);
        Assert.Contains(notifier.Updates, u => u.UserId == userB);

        await service.MarkConversationReadAsync(userB, convo.Id, CancellationToken.None);
        var afterRead = await service.GetConversationAsync(userB, convo.Id, CancellationToken.None);
        Assert.Equal(0, afterRead.UnreadCount);
    }

    [Fact]
    public async Task SendConversationMessage_AllowsImageOnly()
    {
        await using var dbContext = CreateDbContext();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var service = CreateService(dbContext, new FakeTradeReader(new TradeConversationAccessSnapshot(Guid.NewGuid(), userA, userB)), new RecordingNotifier());

        var convo = await service.GetOrCreateDirectConversationAsync(userA, userB, CancellationToken.None);
        var message = await service.SendConversationMessageAsync(userA, convo.Id, new SendMessageRequest(null, "https://cdn/x.png"), CancellationToken.None);

        Assert.Null(message.Body);
        Assert.Equal("https://cdn/x.png", message.ImageUrl);
    }

    [Fact]
    public async Task SendConversationMessage_RejectsEmptyMessage()
    {
        await using var dbContext = CreateDbContext();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var service = CreateService(dbContext, new FakeTradeReader(new TradeConversationAccessSnapshot(Guid.NewGuid(), userA, userB)), new RecordingNotifier());

        var convo = await service.GetOrCreateDirectConversationAsync(userA, userB, CancellationToken.None);

        await Assert.ThrowsAsync<MessagingValidationException>(() =>
            service.SendConversationMessageAsync(userA, convo.Id, new SendMessageRequest(null, null), CancellationToken.None));
    }

    private static MessagingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MessagingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new MessagingDbContext(options);
    }

    private static MessagingService CreateService(
        MessagingDbContext dbContext,
        ITradeMessagingReader tradeReader,
        RecordingNotifier notifier,
        TimeProvider? timeProvider = null)
    {
        return new MessagingService(dbContext, tradeReader, notifier, timeProvider ?? new FixedTimeProvider());
    }

    private sealed class FakeTradeReader : ITradeMessagingReader
    {
        public TradeConversationAccessSnapshot Trade { get; }

        public FakeTradeReader(TradeConversationAccessSnapshot trade)
        {
            Trade = trade;
        }

        public Task<TradeConversationAccessSnapshot?> GetTradeAsync(Guid tradeProposalId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Trade.TradeProposalId == tradeProposalId ? Trade : null);
        }
    }

    private sealed class RecordingNotifier : IConversationRealtimeNotifier
    {
        public List<ConversationMessageResponse> Messages { get; } = new();

        public List<(Guid UserId, ConversationListItemResponse Conversation)> Updates { get; } = new();

        public Task NotifyConversationUpdatedAsync(Guid userId, ConversationListItemResponse conversation, CancellationToken cancellationToken)
        {
            Updates.Add((userId, conversation));
            return Task.CompletedTask;
        }

        public Task NotifyMessageSentAsync(Guid conversationId, ConversationMessageResponse message, CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private DateTimeOffset _current = new(2026, 5, 14, 18, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow()
        {
            var current = _current;
            _current = _current.AddMinutes(1);
            return current;
        }
    }
}
