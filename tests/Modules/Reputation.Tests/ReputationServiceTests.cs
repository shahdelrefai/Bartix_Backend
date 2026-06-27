using Bartrix.Modules.Reputation.Application;
using Bartrix.Modules.Reputation.Contracts;
using Bartrix.Modules.Reputation.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Reputation.Tests;

public sealed class ReputationServiceTests
{
    [Fact]
    public async Task CreateTradeReviewAsync_CreatesReview_ForAcceptedTradeParticipant()
    {
        await using var dbContext = CreateDbContext();
        var reviewerId = Guid.NewGuid();
        var revieweeId = Guid.NewGuid();
        var tradeId = Guid.NewGuid();
        var service = new ReputationService(
            dbContext,
            new FakeTradeReader(new TradeParticipantSnapshot(tradeId, reviewerId, revieweeId, "Accepted")),
            new FixedTimeProvider());

        var response = await service.CreateTradeReviewAsync(
            reviewerId,
            tradeId,
            new CreateReviewRequest(5, "Great trader"),
            CancellationToken.None);

        Assert.Equal(revieweeId, response.RevieweeUserId);
        Assert.Equal(5, response.Rating);
        Assert.True(await dbContext.Reviews.AnyAsync(x => x.TradeProposalId == tradeId));
    }

    [Fact]
    public async Task CreateTradeReviewAsync_RejectsDuplicateReview()
    {
        await using var dbContext = CreateDbContext();
        var reviewerId = Guid.NewGuid();
        var revieweeId = Guid.NewGuid();
        var tradeId = Guid.NewGuid();
        var service = new ReputationService(
            dbContext,
            new FakeTradeReader(new TradeParticipantSnapshot(tradeId, reviewerId, revieweeId, "Accepted")),
            new FixedTimeProvider());

        await service.CreateTradeReviewAsync(reviewerId, tradeId, new CreateReviewRequest(4, null), CancellationToken.None);

        await Assert.ThrowsAsync<ReputationValidationException>(() =>
            service.CreateTradeReviewAsync(reviewerId, tradeId, new CreateReviewRequest(5, "Second"), CancellationToken.None));
    }

    [Fact]
    public async Task CreateTradeReviewAsync_RejectsNonAcceptedTrade()
    {
        await using var dbContext = CreateDbContext();
        var reviewerId = Guid.NewGuid();
        var revieweeId = Guid.NewGuid();
        var tradeId = Guid.NewGuid();
        var service = new ReputationService(
            dbContext,
            new FakeTradeReader(new TradeParticipantSnapshot(tradeId, reviewerId, revieweeId, "Pending")),
            new FixedTimeProvider());

        await Assert.ThrowsAsync<ReputationValidationException>(() =>
            service.CreateTradeReviewAsync(reviewerId, tradeId, new CreateReviewRequest(5, null), CancellationToken.None));
    }

    [Fact]
    public async Task GetUserReputationAsync_ReturnsAggregate()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var reviewerA = Guid.NewGuid();
        var reviewerB = Guid.NewGuid();
        var service = new ReputationService(dbContext, new FakeTradeReader(null), new FixedTimeProvider());

        dbContext.Reviews.AddRange(
            new Domain.ReputationReview(Guid.NewGuid(), reviewerA, userId, 5, "Excellent", new DateTimeOffset(2026, 5, 14, 10, 0, 0, TimeSpan.Zero)),
            new Domain.ReputationReview(Guid.NewGuid(), reviewerB, userId, 3, "Okay", new DateTimeOffset(2026, 5, 14, 11, 0, 0, TimeSpan.Zero)));
        await dbContext.SaveChangesAsync();

        var summary = await service.GetUserReputationAsync(userId, CancellationToken.None);

        Assert.Equal(2, summary.TotalReviews);
        Assert.Equal(4.00m, summary.AverageRating);
        Assert.Equal("Okay", summary.RecentReviews[0].Comment);
    }

    [Fact]
    public async Task CreateUserReviewAsync_CreatesDirectReview_AndCountsInReputation()
    {
        await using var dbContext = CreateDbContext();
        var reviewerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var service = new ReputationService(dbContext, new FakeTradeReader(null), new FixedTimeProvider());

        var review = await service.CreateUserReviewAsync(
            reviewerId,
            targetId,
            new CreateUserReviewRequest(5, "Reliable", "Smooth and friendly", "Alice"),
            CancellationToken.None);

        Assert.Null(review.TradeProposalId);
        Assert.Equal("Reliable", review.Title);
        Assert.Equal("Alice", review.ReviewerName);
        Assert.Equal("Smooth and friendly", review.Comment);

        var reviews = await service.GetUserReviewsAsync(targetId, CancellationToken.None);
        Assert.Single(reviews);

        var summary = await service.GetUserReputationAsync(targetId, CancellationToken.None);
        Assert.Equal(1, summary.TotalReviews);
        Assert.Equal(5.00m, summary.AverageRating);
    }

    [Fact]
    public async Task CreateUserReviewAsync_RejectsSelfReview()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var service = new ReputationService(dbContext, new FakeTradeReader(null), new FixedTimeProvider());

        await Assert.ThrowsAsync<ReputationValidationException>(() =>
            service.CreateUserReviewAsync(userId, userId, new CreateUserReviewRequest(4, null, "Nope"), CancellationToken.None));
    }

    [Fact]
    public async Task CreateUserReviewAsync_AllowsMultipleReviewsForSameTarget()
    {
        await using var dbContext = CreateDbContext();
        var targetId = Guid.NewGuid();
        var service = new ReputationService(dbContext, new FakeTradeReader(null), new FixedTimeProvider());

        await service.CreateUserReviewAsync(Guid.NewGuid(), targetId, new CreateUserReviewRequest(5, null, "A"), CancellationToken.None);
        await service.CreateUserReviewAsync(Guid.NewGuid(), targetId, new CreateUserReviewRequest(3, null, "B"), CancellationToken.None);

        var reviews = await service.GetUserReviewsAsync(targetId, CancellationToken.None);
        Assert.Equal(2, reviews.Count);
    }

    private static ReputationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ReputationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ReputationDbContext(options);
    }

    private sealed class FakeTradeReader : ITradeReputationReader
    {
        private readonly TradeParticipantSnapshot? _trade;

        public FakeTradeReader(TradeParticipantSnapshot? trade)
        {
            _trade = trade;
        }

        public Task<TradeParticipantSnapshot?> GetTradeAsync(Guid tradeProposalId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_trade?.TradeProposalId == tradeProposalId ? _trade : null);
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private static readonly DateTimeOffset FixedUtcNow = new(2026, 5, 14, 19, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => FixedUtcNow;
    }
}
