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
