using Bartrix.BuildingBlocks.Notifications;
using Bartrix.Modules.Notifications.Application;
using Bartrix.Modules.Notifications.Contracts;
using Bartrix.Modules.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Notifications.Tests;

public sealed class NotificationServiceTests
{
    // ── Template rendering ────────────────────────────────────────────────────

    [Fact]
    public void Render_ReturnsEnglishTemplate_WhenLanguageIsEn()
    {
        var result = NotificationTemplates.Render("en", "newOfferTitle", null);
        Assert.Equal("New Trade Offer", result);
    }

    [Fact]
    public void Render_ReturnsArabicTemplate_WhenLanguageIsAr()
    {
        var result = NotificationTemplates.Render("ar", "newOfferTitle", null);
        Assert.Equal("عرض مبادلة جديد", result);
    }

    [Fact]
    public void Render_FallsBackToEnglish_WhenLanguageUnknown()
    {
        var result = NotificationTemplates.Render("fr", "newOfferTitle", null);
        Assert.Equal("New Trade Offer", result);
    }

    [Fact]
    public void Render_SubstitutesPlaceholders()
    {
        var args = new Dictionary<string, string> { ["sender"] = "Alice" };
        var result = NotificationTemplates.Render("en", "newOfferBody", args);
        Assert.Equal("Alice sent you a trade offer.", result);
    }

    [Fact]
    public void Render_ReturnsRawKey_WhenKeyNotFound()
    {
        var result = NotificationTemplates.Render("en", "unknownKey", null);
        Assert.Equal("unknownKey", result);
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_PersistsNotification_AndNotifiesRealtime()
    {
        await using var db = CreateDbContext();
        var notifier = new RecordingNotifier();
        var service = CreateService(db, notifier);
        var userId = Guid.NewGuid();

        var response = await service.CreateAsync(
            userId, "Title", "Body", "tradeUpdate", null, CancellationToken.None);

        Assert.Equal(userId, response.UserId);
        Assert.Equal("Title", response.Title);
        Assert.False(response.IsRead);
        Assert.Single(notifier.Calls);
        Assert.Equal(userId, notifier.Calls[0].UserId);
        Assert.Equal(1, notifier.Calls[0].UnreadCount);
        Assert.True(await db.Notifications.AnyAsync(x => x.Id == response.Id));
    }

    // ── GetUnreadCountAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetUnreadCountAsync_CountsOnlyUnread()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var userId = Guid.NewGuid();

        await service.CreateAsync(userId, "A", "B", "system", null, CancellationToken.None);
        var first = await service.CreateAsync(userId, "C", "D", "system", null, CancellationToken.None);
        await service.MarkReadAsync(userId, first.Id, CancellationToken.None);

        var count = await service.GetUnreadCountAsync(userId, CancellationToken.None);
        Assert.Equal(1, count);
    }

    // ── MarkReadAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task MarkReadAsync_MarksNotificationRead()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var userId = Guid.NewGuid();
        var created = await service.CreateAsync(userId, "T", "B", "chatMessage", null, CancellationToken.None);

        await service.MarkReadAsync(userId, created.Id, CancellationToken.None);

        var stored = await db.Notifications.SingleAsync(x => x.Id == created.Id);
        Assert.True(stored.IsRead);
    }

    [Fact]
    public async Task MarkReadAsync_Throws_WhenNotificationBelongsToDifferentUser()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var ownerUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var created = await service.CreateAsync(ownerUserId, "T", "B", "system", null, CancellationToken.None);

        await Assert.ThrowsAsync<NotificationValidationException>(
            () => service.MarkReadAsync(otherUserId, created.Id, CancellationToken.None));
    }

    // ── MarkAllReadAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task MarkAllReadAsync_MarksAllUnreadForUser()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var userId = Guid.NewGuid();

        await service.CreateAsync(userId, "A", "B", "system", null, CancellationToken.None);
        await service.CreateAsync(userId, "C", "D", "system", null, CancellationToken.None);

        await service.MarkAllReadAsync(userId, CancellationToken.None);

        var unread = await db.Notifications.CountAsync(x => x.UserId == userId && !x.IsRead);
        Assert.Equal(0, unread);
    }

    // ── PublishAsync (INotificationPublisher) ─────────────────────────────────

    [Fact]
    public async Task PublishAsync_RendersTemplateAndPersists_EnglishUser()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var notifier = new RecordingNotifier();
        var service = CreateService(db, notifier, languageCode: "en");

        await ((INotificationPublisher)service).PublishAsync(new NotificationPublishRequest(
            UserId: userId,
            TitleKey: "newOfferTitle",
            BodyKey: "newOfferBody",
            Type: "tradeUpdate",
            BodyArgs: new Dictionary<string, string> { ["sender"] = "Bob" }),
            CancellationToken.None);

        var stored = await db.Notifications.SingleAsync(x => x.UserId == userId);
        Assert.Equal("New Trade Offer", stored.Title);
        Assert.Equal("Bob sent you a trade offer.", stored.Body);
    }

    [Fact]
    public async Task PublishAsync_RendersArabicTemplate_WhenUserLanguageIsAr()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var service = CreateService(db, languageCode: "ar");

        await ((INotificationPublisher)service).PublishAsync(new NotificationPublishRequest(
            UserId: userId,
            TitleKey: "tradeAcceptedTitle",
            BodyKey: "tradeAcceptedBody",
            Type: "tradeUpdate"),
            CancellationToken.None);

        var stored = await db.Notifications.SingleAsync(x => x.UserId == userId);
        Assert.Equal("تم قبول المبادلة!", stored.Title);
    }

    // ── GetForUserAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetForUserAsync_ReturnsOnlyCurrentUsersNotifications()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        await service.CreateAsync(userA, "A1", "B1", "system", null, CancellationToken.None);
        await service.CreateAsync(userA, "A2", "B2", "system", null, CancellationToken.None);
        await service.CreateAsync(userB, "B1", "B1", "system", null, CancellationToken.None);

        var results = await service.GetForUserAsync(userA, CancellationToken.None);
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(userA, r.UserId));
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static NotificationsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new NotificationsDbContext(options);
    }

    private static NotificationService CreateService(
        NotificationsDbContext db,
        RecordingNotifier? notifier = null,
        string languageCode = "en")
    {
        return new NotificationService(
            db,
            notifier ?? new RecordingNotifier(),
            new StubLanguageReader(languageCode),
            new FixedTimeProvider());
    }

    private sealed class StubLanguageReader : IUserLanguageReader
    {
        private readonly string _lang;
        public StubLanguageReader(string lang) => _lang = lang;
        public Task<string> GetLanguageCodeAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(_lang);
    }

    private sealed class RecordingNotifier : INotificationRealtimeNotifier
    {
        public List<(Guid UserId, NotificationResponse Notification, int UnreadCount)> Calls { get; } = new();

        public Task NotifyAsync(Guid userId, NotificationResponse notification, int unreadCount, CancellationToken cancellationToken)
        {
            Calls.Add((userId, notification, unreadCount));
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private static readonly DateTimeOffset FixedUtcNow = new(2026, 5, 14, 19, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => FixedUtcNow;
    }
}
