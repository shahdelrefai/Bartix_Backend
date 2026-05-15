using Bartrix.Modules.Delivery.Application;
using Bartrix.Modules.Delivery.Contracts;
using Bartrix.Modules.Delivery.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Delivery.Tests;

public sealed class DeliveryServiceTests
{
    [Fact]
    public async Task GetTradeDeliveryAsync_CreatesDelivery_ForAcceptedTrade()
    {
        await using var dbContext = CreateDbContext();
        var tradeId = Guid.NewGuid();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var service = CreateService(
            dbContext,
            new FakeTradeReader(new TradeDeliveryAccessSnapshot(tradeId, userA, userB, "Accepted")));

        var response = await service.GetTradeDeliveryAsync(userA, tradeId, CancellationToken.None);

        Assert.Equal("Pending", response.Status);
        Assert.Equal(tradeId, response.TradeProposalId);
    }

    [Fact]
    public async Task UpdateTradeDeliveryAsync_SchedulesMeetup()
    {
        await using var dbContext = CreateDbContext();
        var tradeId = Guid.NewGuid();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var service = CreateService(
            dbContext,
            new FakeTradeReader(new TradeDeliveryAccessSnapshot(tradeId, userA, userB, "Accepted")));

        var response = await service.UpdateTradeDeliveryAsync(
            userA,
            tradeId,
            new UpdateDeliveryRequest("Meetup", "Nasr City, Cairo", new DateTimeOffset(2026, 5, 15, 18, 0, 0, TimeSpan.Zero), "Cafe entrance"),
            CancellationToken.None);

        Assert.Equal("Meetup", response.Method);
        Assert.Equal("Scheduled", response.Status);
        Assert.Equal("Nasr City, Cairo", response.Location);
    }

    [Fact]
    public async Task MarkDeliveredAndConfirm_CompletesDelivery()
    {
        await using var dbContext = CreateDbContext();
        var tradeId = Guid.NewGuid();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var service = CreateService(
            dbContext,
            new FakeTradeReader(new TradeDeliveryAccessSnapshot(tradeId, userA, userB, "Accepted")));

        await service.UpdateTradeDeliveryAsync(
            userA,
            tradeId,
            new UpdateDeliveryRequest("DigitalService", null, null, "Send final files by email"),
            CancellationToken.None);

        var delivered = await service.MarkDeliveredAsync(userA, tradeId, CancellationToken.None);
        var confirmed = await service.ConfirmAsync(userB, tradeId, CancellationToken.None);

        Assert.Equal("Delivered", delivered.Status);
        Assert.Equal("Confirmed", confirmed.Status);
    }

    [Fact]
    public async Task UpdateTradeDeliveryAsync_RejectsPendingTrade()
    {
        await using var dbContext = CreateDbContext();
        var tradeId = Guid.NewGuid();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var service = CreateService(
            dbContext,
            new FakeTradeReader(new TradeDeliveryAccessSnapshot(tradeId, userA, userB, "Pending")));

        await Assert.ThrowsAsync<DeliveryValidationException>(() =>
            service.UpdateTradeDeliveryAsync(
                userA,
                tradeId,
                new UpdateDeliveryRequest("Dropoff", "Reception desk", new DateTimeOffset(2026, 5, 15, 10, 0, 0, TimeSpan.Zero), null),
                CancellationToken.None));
    }

    private static DeliveryDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DeliveryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new DeliveryDbContext(options);
    }

    private static DeliveryService CreateService(DeliveryDbContext dbContext, ITradeDeliveryReader tradeReader)
    {
        return new DeliveryService(dbContext, tradeReader, new FixedTimeProvider());
    }

    private sealed class FakeTradeReader : ITradeDeliveryReader
    {
        private readonly TradeDeliveryAccessSnapshot _trade;

        public FakeTradeReader(TradeDeliveryAccessSnapshot trade)
        {
            _trade = trade;
        }

        public Task<TradeDeliveryAccessSnapshot?> GetTradeAsync(Guid tradeProposalId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_trade.TradeProposalId == tradeProposalId ? _trade : null);
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private DateTimeOffset _current = new(2026, 5, 14, 20, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow()
        {
            var current = _current;
            _current = _current.AddMinutes(1);
            return current;
        }
    }
}
