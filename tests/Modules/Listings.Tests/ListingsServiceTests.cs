using Bartrix.Modules.Listings.Application;
using Bartrix.Modules.Listings.Contracts;
using Bartrix.Modules.Listings.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Listings.Tests;

public sealed class ListingsServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesListing_WithImages()
    {
        await using var dbContext = CreateDbContext();
        var service = new ListingsService(dbContext, new FixedTimeProvider());

        var response = await service.CreateAsync(
            Guid.NewGuid(),
            new CreateListingRequest(
                "MacBook Pro",
                "Used laptop in good condition",
                "Electronics",
                "Used",
                "Cairo",
                25000m,
                true,
                true,
                new[] { "https://cdn.bartrix.dev/l1.png", "https://cdn.bartrix.dev/l2.png" }),
            CancellationToken.None);

        Assert.Equal("MacBook Pro", response.Title);
        Assert.Equal(2, response.Images.Count);
        Assert.True(response.IsAvailableForSale);
    }

    [Fact]
    public async Task BrowseAsync_FiltersByCategory_AndOnlyActive()
    {
        await using var dbContext = CreateDbContext();
        var service = new ListingsService(dbContext, new FixedTimeProvider());
        var ownerId = Guid.NewGuid();

        var active = await service.CreateAsync(
            ownerId,
            new CreateListingRequest("Chair", "Wooden chair", "Furniture", "Used", "Cairo", null, true, false, null),
            CancellationToken.None);

        var archived = await service.CreateAsync(
            ownerId,
            new CreateListingRequest("Phone", "Old phone", "Electronics", "Used", "Giza", 1000m, false, true, null),
            CancellationToken.None);

        await service.ArchiveAsync(ownerId, archived.Id, CancellationToken.None);

        var response = await service.BrowseAsync(
            new ListingsQuery { Category = "Furniture", OnlyActive = true },
            CancellationToken.None);

        Assert.Single(response.Items);
        Assert.Equal(active.Id, response.Items[0].Id);
    }

    [Fact]
    public async Task UpdateAsync_RejectsNonOwner()
    {
        await using var dbContext = CreateDbContext();
        var service = new ListingsService(dbContext, new FixedTimeProvider());
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var listing = await service.CreateAsync(
            ownerId,
            new CreateListingRequest("Desk", "Office desk", "Furniture", "Used", "Alexandria", null, true, false, null),
            CancellationToken.None);

        await Assert.ThrowsAsync<ListingsValidationException>(() =>
            service.UpdateAsync(
                otherUserId,
                listing.Id,
                new UpdateListingRequest("Desk Updated", "Office desk", "Furniture", "Used", "Alexandria", null, true, false, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_RequiresPrice_WhenAvailableForSale()
    {
        await using var dbContext = CreateDbContext();
        var service = new ListingsService(dbContext, new FixedTimeProvider());

        await Assert.ThrowsAsync<ListingsValidationException>(() =>
            service.CreateAsync(
                Guid.NewGuid(),
                new CreateListingRequest("Tablet", "Almost new tablet", "Electronics", "New", "Cairo", null, false, true, null),
                CancellationToken.None));
    }

    private static ListingsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ListingsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ListingsDbContext(options);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private static readonly DateTimeOffset FixedUtcNow = new(2026, 5, 14, 14, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => FixedUtcNow;
    }
}
