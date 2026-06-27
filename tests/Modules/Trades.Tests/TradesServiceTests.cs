using Bartrix.Modules.Trades.Application;
using Bartrix.Modules.Trades.Contracts;
using Bartrix.Modules.Trades.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Trades.Tests;

public sealed class TradesServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesPendingTrade()
    {
        await using var dbContext = CreateDbContext();
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var requestedListingId = Guid.NewGuid();
        var offeredListingId = Guid.NewGuid();
        var service = new TradesService(
            dbContext,
            new FakeListingTradeValidationReader(
                new[]
                {
                    new ListingOwnershipSnapshot(requestedListingId, receiverId, true),
                    new ListingOwnershipSnapshot(offeredListingId, senderId, true)
                }),
            new FixedTimeProvider());

        var response = await service.CreateAsync(
            senderId,
            new CreateTradeProposalRequest(requestedListingId, new[] { offeredListingId }, "Interested in swapping"),
            CancellationToken.None);

        Assert.Equal("Pending", response.Status);
        Assert.Single(response.OfferedListingIds);
        Assert.Equal(receiverId, response.ReceiverUserId);
    }

    [Fact]
    public async Task AcceptAsync_ChangesStatusToAccepted()
    {
        await using var dbContext = CreateDbContext();
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var requestedListingId = Guid.NewGuid();
        var offeredListingId = Guid.NewGuid();
        var service = new TradesService(
            dbContext,
            new FakeListingTradeValidationReader(
                new[]
                {
                    new ListingOwnershipSnapshot(requestedListingId, receiverId, true),
                    new ListingOwnershipSnapshot(offeredListingId, senderId, true)
                }),
            new FixedTimeProvider());

        var created = await service.CreateAsync(
            senderId,
            new CreateTradeProposalRequest(requestedListingId, new[] { offeredListingId }, null),
            CancellationToken.None);

        var accepted = await service.AcceptAsync(receiverId, created.Id, CancellationToken.None);

        Assert.Equal("Accepted", accepted.Status);
    }

    [Fact]
    public async Task RejectAsync_RequiresReceiver()
    {
        await using var dbContext = CreateDbContext();
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var requestedListingId = Guid.NewGuid();
        var offeredListingId = Guid.NewGuid();
        var service = new TradesService(
            dbContext,
            new FakeListingTradeValidationReader(
                new[]
                {
                    new ListingOwnershipSnapshot(requestedListingId, receiverId, true),
                    new ListingOwnershipSnapshot(offeredListingId, senderId, true)
                }),
            new FixedTimeProvider());

        var created = await service.CreateAsync(
            senderId,
            new CreateTradeProposalRequest(requestedListingId, new[] { offeredListingId }, null),
            CancellationToken.None);

        await Assert.ThrowsAsync<TradesValidationException>(() =>
            service.RejectAsync(senderId, created.Id, new RejectTradeRequest(null), CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_RejectsOfferingOtherUsersListings()
    {
        await using var dbContext = CreateDbContext();
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var requestedListingId = Guid.NewGuid();
        var offeredListingId = Guid.NewGuid();
        var service = new TradesService(
            dbContext,
            new FakeListingTradeValidationReader(
                new[]
                {
                    new ListingOwnershipSnapshot(requestedListingId, receiverId, true),
                    new ListingOwnershipSnapshot(offeredListingId, Guid.NewGuid(), true)
                }),
            new FixedTimeProvider());

        await Assert.ThrowsAsync<TradesValidationException>(() =>
            service.CreateAsync(
                senderId,
                new CreateTradeProposalRequest(requestedListingId, new[] { offeredListingId }, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task AcceptAsync_AutoRejectsCompetingPendingTrades()
    {
        await using var dbContext = CreateDbContext();
        var receiverId = Guid.NewGuid();
        var requestedListingId = Guid.NewGuid();
        var senderA = Guid.NewGuid();
        var offeredA = Guid.NewGuid();
        var senderB = Guid.NewGuid();
        var offeredB = Guid.NewGuid();

        var service = new TradesService(
            dbContext,
            new FakeListingTradeValidationReader(new[]
            {
                new ListingOwnershipSnapshot(requestedListingId, receiverId, true),
                new ListingOwnershipSnapshot(offeredA, senderA, true),
                new ListingOwnershipSnapshot(offeredB, senderB, true)
            }),
            new FixedTimeProvider());

        var tradeA = await service.CreateAsync(senderA,
            new CreateTradeProposalRequest(requestedListingId, new[] { offeredA }, null), CancellationToken.None);
        var tradeB = await service.CreateAsync(senderB,
            new CreateTradeProposalRequest(requestedListingId, new[] { offeredB }, null), CancellationToken.None);

        await service.AcceptAsync(receiverId, tradeA.Id, CancellationToken.None);

        var rejectedB = await service.GetByIdAsync(receiverId, tradeB.Id, CancellationToken.None);
        Assert.Equal("Rejected", rejectedB!.Status);
        Assert.False(string.IsNullOrEmpty(rejectedB.RejectionReason));
    }

    [Fact]
    public async Task CounterOffer_AddedThenAccepted_SettlesTrade()
    {
        await using var dbContext = CreateDbContext();
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var requestedListingId = Guid.NewGuid();
        var offeredListingId = Guid.NewGuid();
        var receiverListing = Guid.NewGuid();

        var service = new TradesService(
            dbContext,
            new FakeListingTradeValidationReader(new[]
            {
                new ListingOwnershipSnapshot(requestedListingId, receiverId, true),
                new ListingOwnershipSnapshot(offeredListingId, senderId, true),
                new ListingOwnershipSnapshot(receiverListing, receiverId, true)
            }),
            new FixedTimeProvider());

        var trade = await service.CreateAsync(senderId,
            new CreateTradeProposalRequest(requestedListingId, new[] { offeredListingId }, null), CancellationToken.None);

        // Receiver counters with their own listing.
        var counter = await service.AddCounterOfferAsync(receiverId, trade.Id,
            new AddCounterOfferRequest(new[] { receiverListing }, new[] { offeredListingId }, "How about this?"),
            CancellationToken.None);

        Assert.Equal(receiverId, counter.FromUserId);
        Assert.Equal(senderId, counter.ToUserId);

        // Sender accepts the counter-offer.
        var settled = await service.AcceptCounterOfferAsync(senderId, trade.Id, counter.Id, CancellationToken.None);
        Assert.Equal("Accepted", settled.Status);

        var history = await service.GetHistoryAsync(senderId, trade.Id, CancellationToken.None);
        Assert.Contains(history, h => h.Action == "COUNTER_OFFER_ACCEPTED");
    }

    [Fact]
    public async Task ExpireOverdueAsync_ExpiresPastDuePendingTrades()
    {
        await using var dbContext = CreateDbContext();
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var requestedListingId = Guid.NewGuid();
        var offeredListingId = Guid.NewGuid();
        var time = new MutableTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var service = new TradesService(
            dbContext,
            new FakeListingTradeValidationReader(new[]
            {
                new ListingOwnershipSnapshot(requestedListingId, receiverId, true),
                new ListingOwnershipSnapshot(offeredListingId, senderId, true)
            }),
            time);

        var trade = await service.CreateAsync(senderId,
            new CreateTradeProposalRequest(requestedListingId, new[] { offeredListingId }, null, ExpiresInHours: 1),
            CancellationToken.None);

        time.Advance(TimeSpan.FromHours(2));
        var expiredCount = await service.ExpireOverdueAsync(CancellationToken.None);

        Assert.Equal(1, expiredCount);
        var fetched = await service.GetByIdAsync(receiverId, trade.Id, CancellationToken.None);
        Assert.Equal("Expired", fetched!.Status);
    }

    [Fact]
    public async Task CompleteAsync_RequiresAcceptedTrade()
    {
        await using var dbContext = CreateDbContext();
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var requestedListingId = Guid.NewGuid();
        var offeredListingId = Guid.NewGuid();

        var service = new TradesService(
            dbContext,
            new FakeListingTradeValidationReader(new[]
            {
                new ListingOwnershipSnapshot(requestedListingId, receiverId, true),
                new ListingOwnershipSnapshot(offeredListingId, senderId, true)
            }),
            new FixedTimeProvider());

        var trade = await service.CreateAsync(senderId,
            new CreateTradeProposalRequest(requestedListingId, new[] { offeredListingId }, null), CancellationToken.None);

        await Assert.ThrowsAsync<TradesValidationException>(() =>
            service.CompleteAsync(senderId, trade.Id, CancellationToken.None));

        await service.AcceptAsync(receiverId, trade.Id, CancellationToken.None);
        var completed = await service.CompleteAsync(senderId, trade.Id, CancellationToken.None);
        Assert.Equal("Completed", completed.Status);
    }

    private sealed class MutableTimeProvider : TimeProvider
    {
        private DateTimeOffset _now;
        public MutableTimeProvider(DateTimeOffset now) => _now = now;
        public void Advance(TimeSpan by) => _now = _now.Add(by);
        public override DateTimeOffset GetUtcNow() => _now;
    }

    private static TradesDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TradesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new TradesDbContext(options);
    }

    private sealed class FakeListingTradeValidationReader : IListingTradeValidationReader
    {
        private readonly Dictionary<Guid, ListingOwnershipSnapshot> _items;

        public FakeListingTradeValidationReader(IEnumerable<ListingOwnershipSnapshot> items)
        {
            _items = items.ToDictionary(x => x.ListingId);
        }

        public Task<ListingOwnershipSnapshot?> GetListingAsync(Guid listingId, CancellationToken cancellationToken)
        {
            _items.TryGetValue(listingId, out var item);
            return Task.FromResult(item);
        }

        public Task<IReadOnlyList<ListingOwnershipSnapshot>> GetListingsAsync(IEnumerable<Guid> listingIds, CancellationToken cancellationToken)
        {
            var items = listingIds.Where(_items.ContainsKey).Select(id => _items[id]).ToList();
            return Task.FromResult<IReadOnlyList<ListingOwnershipSnapshot>>(items);
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private static readonly DateTimeOffset FixedUtcNow = new(2026, 5, 14, 16, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => FixedUtcNow;
    }
}
