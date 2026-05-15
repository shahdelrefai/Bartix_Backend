using Bartrix.Modules.Services.Application;
using Bartrix.Modules.Services.Contracts;
using Bartrix.Modules.Services.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Services.Tests;

public sealed class ServicesServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesExchangeOnlyService()
    {
        await using var dbContext = CreateDbContext();
        var service = new ServicesService(dbContext, new FixedTimeProvider());

        var response = await service.CreateAsync(
            Guid.NewGuid(),
            new CreateServiceOfferRequest(
                "Logo Design",
                "Simple startup logo package",
                "Design",
                "Cairo",
                "Remote",
                "ExchangeOnly",
                null,
                true),
            CancellationToken.None);

        Assert.Equal("Logo Design", response.Title);
        Assert.Equal("ExchangeOnly", response.PricingType);
        Assert.True(response.IsAvailableForTrade);
    }

    [Fact]
    public async Task BrowseAsync_FiltersByFulfillmentMode_AndOnlyActive()
    {
        await using var dbContext = CreateDbContext();
        var service = new ServicesService(dbContext, new FixedTimeProvider());
        var ownerId = Guid.NewGuid();

        var active = await service.CreateAsync(
            ownerId,
            new CreateServiceOfferRequest(
                "Photography Session",
                "Outdoor portrait session",
                "Photography",
                "Cairo",
                "OnSite",
                "FixedPrice",
                1500m,
                true),
            CancellationToken.None);

        var archived = await service.CreateAsync(
            ownerId,
            new CreateServiceOfferRequest(
                "Translation",
                "English to Arabic translation",
                "Writing",
                "Giza",
                "Remote",
                "Hourly",
                250m,
                false),
            CancellationToken.None);

        await service.ArchiveAsync(ownerId, archived.Id, CancellationToken.None);

        var response = await service.BrowseAsync(
            new ServicesQuery { FulfillmentMode = "OnSite", OnlyActive = true },
            CancellationToken.None);

        Assert.Single(response.Items);
        Assert.Equal(active.Id, response.Items[0].Id);
    }

    [Fact]
    public async Task UpdateAsync_RejectsNonOwner()
    {
        await using var dbContext = CreateDbContext();
        var service = new ServicesService(dbContext, new FixedTimeProvider());
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var serviceOffer = await service.CreateAsync(
            ownerId,
            new CreateServiceOfferRequest(
                "Furniture Assembly",
                "Home office setup support",
                "Home Services",
                "Alexandria",
                "OnSite",
                "Hourly",
                300m,
                false),
            CancellationToken.None);

        await Assert.ThrowsAsync<ServicesValidationException>(() =>
            service.UpdateAsync(
                otherUserId,
                serviceOffer.Id,
                new UpdateServiceOfferRequest(
                    "Furniture Assembly",
                    "Updated description",
                    "Home Services",
                    "Alexandria",
                    "Hybrid",
                    "Hourly",
                    350m,
                    false),
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_RequiresPrice_ForFixedPriceService()
    {
        await using var dbContext = CreateDbContext();
        var service = new ServicesService(dbContext, new FixedTimeProvider());

        await Assert.ThrowsAsync<ServicesValidationException>(() =>
            service.CreateAsync(
                Guid.NewGuid(),
                new CreateServiceOfferRequest(
                    "Tutoring",
                    "Math tutoring for high school students",
                    "Education",
                    "Cairo",
                    "Hybrid",
                    "FixedPrice",
                    null,
                    true),
                CancellationToken.None));
    }

    private static ServicesDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ServicesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ServicesDbContext(options);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private static readonly DateTimeOffset FixedUtcNow = new(2026, 5, 14, 14, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => FixedUtcNow;
    }
}
