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
            service.RejectAsync(senderId, created.Id, CancellationToken.None));
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
