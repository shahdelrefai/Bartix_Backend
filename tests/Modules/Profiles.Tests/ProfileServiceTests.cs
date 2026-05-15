using Bartrix.Modules.Profiles.Application;
using Bartrix.Modules.Profiles.Contracts;
using Bartrix.Modules.Profiles.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Profiles.Tests;

public sealed class ProfileServiceTests
{
    [Fact]
    public async Task GetMyProfileAsync_CreatesDefaultProfile_WhenMissing()
    {
        await using var dbContext = CreateDbContext();
        var service = new ProfileService(dbContext, new FixedTimeProvider());
        var userId = Guid.NewGuid();

        var response = await service.GetMyProfileAsync(userId, "Claim Name", CancellationToken.None);

        Assert.Equal(userId, response.UserId);
        Assert.Equal("Claim Name", response.DisplayName);
        Assert.True(await dbContext.UserProfiles.AnyAsync(x => x.UserId == userId));
    }

    [Fact]
    public async Task UpdateMyProfileAsync_UpsertsProfileValues()
    {
        await using var dbContext = CreateDbContext();
        var service = new ProfileService(dbContext, new FixedTimeProvider());
        var userId = Guid.NewGuid();

        var response = await service.UpdateMyProfileAsync(
            userId,
            "Initial Name",
            new UpdateMyProfileRequest("Updated Name", "Bio", "Cairo", "https://cdn.bartrix.dev/avatar.png"),
            CancellationToken.None);

        Assert.Equal("Updated Name", response.DisplayName);
        Assert.Equal("Bio", response.Bio);
        Assert.Equal("Cairo", response.Location);
        Assert.Equal("https://cdn.bartrix.dev/avatar.png", response.AvatarUrl);
    }

    [Fact]
    public async Task UpdateMyProfileAsync_RejectsTooLongDisplayName()
    {
        await using var dbContext = CreateDbContext();
        var service = new ProfileService(dbContext, new FixedTimeProvider());

        var request = new UpdateMyProfileRequest(new string('a', 201), null, null, null);

        await Assert.ThrowsAsync<ProfileValidationException>(() =>
            service.UpdateMyProfileAsync(Guid.NewGuid(), "Name", request, CancellationToken.None));
    }

    private static ProfilesDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ProfilesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ProfilesDbContext(options);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private static readonly DateTimeOffset FixedUtcNow = new(2026, 5, 14, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => FixedUtcNow;
    }
}
