using Bartrix.Modules.Reputation.Contracts;
using Bartrix.Modules.Reputation.Domain;
using Bartrix.Modules.Reputation.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Reputation.Application;

public sealed class ReputationService : IReputationService
{
    private readonly ReputationDbContext _dbContext;
    private readonly ITradeReputationReader _tradeReader;
    private readonly TimeProvider _timeProvider;

    public ReputationService(
        ReputationDbContext dbContext,
        ITradeReputationReader tradeReader,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _tradeReader = tradeReader;
        _timeProvider = timeProvider;
    }

    public async Task<ReviewResponse> CreateTradeReviewAsync(
        Guid reviewerUserId,
        Guid tradeProposalId,
        CreateReviewRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Rating < 1 || request.Rating > 5)
        {
            throw new ReputationValidationException("Rating must be between 1 and 5.");
        }

        var trade = await _tradeReader.GetTradeAsync(tradeProposalId, cancellationToken);
        if (trade is null)
        {
            throw new ReputationValidationException("Trade proposal was not found.");
        }

        if (trade.SenderUserId != reviewerUserId && trade.ReceiverUserId != reviewerUserId)
        {
            throw new ReputationValidationException("Only trade participants can leave a review.");
        }

        if (!string.Equals(trade.Status, "Accepted", StringComparison.OrdinalIgnoreCase))
        {
            throw new ReputationValidationException("Reviews can only be left for accepted trades.");
        }

        var revieweeUserId = trade.SenderUserId == reviewerUserId ? trade.ReceiverUserId : trade.SenderUserId;

        var existing = await _dbContext.Reviews.SingleOrDefaultAsync(
            x => x.TradeProposalId == tradeProposalId && x.ReviewerUserId == reviewerUserId,
            cancellationToken);

        if (existing is not null)
        {
            throw new ReputationValidationException("You have already reviewed this trade.");
        }

        var comment = NormalizeComment(request.Comment);
        var review = new ReputationReview(
            tradeProposalId,
            reviewerUserId,
            revieweeUserId,
            request.Rating,
            comment,
            _timeProvider.GetUtcNow());

        _dbContext.Reviews.Add(review);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Map(review);
    }

    public async Task<ReputationSummaryResponse> GetUserReputationAsync(Guid userId, CancellationToken cancellationToken)
    {
        var query = _dbContext.Reviews
            .AsNoTracking()
            .Where(x => x.RevieweeUserId == userId);

        var totalReviews = await query.CountAsync(cancellationToken);
        var averageRating = totalReviews == 0
            ? 0m
            : await query.AverageAsync(x => (decimal)x.Rating, cancellationToken);

        var recentReviews = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        return new ReputationSummaryResponse(
            userId,
            totalReviews,
            Math.Round(averageRating, 2, MidpointRounding.AwayFromZero),
            recentReviews.Select(Map).ToList());
    }

    public async Task<ReviewResponse> CreateUserReviewAsync(
        Guid reviewerUserId,
        Guid targetUserId,
        CreateUserReviewRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Rating < 1 || request.Rating > 5)
        {
            throw new ReputationValidationException("Rating must be between 1 and 5.");
        }

        if (reviewerUserId == targetUserId)
        {
            throw new ReputationValidationException("You cannot review yourself.");
        }

        var title = NormalizeTitle(request.Title);
        var content = NormalizeComment(request.Content);
        var reviewerName = string.IsNullOrWhiteSpace(request.ReviewerName) ? null : request.ReviewerName.Trim();

        var review = new ReputationReview(
            tradeProposalId: null,
            reviewerUserId,
            targetUserId,
            request.Rating,
            content,
            _timeProvider.GetUtcNow(),
            reviewerName,
            title);

        _dbContext.Reviews.Add(review);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Map(review);
    }

    public async Task<IReadOnlyList<ReviewResponse>> GetUserReviewsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var reviews = await _dbContext.Reviews
            .AsNoTracking()
            .Where(x => x.RevieweeUserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return reviews.Select(Map).ToList();
    }

    private static string? NormalizeTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var normalized = title.Trim();
        if (normalized.Length > 200)
        {
            throw new ReputationValidationException("Title cannot exceed 200 characters.");
        }

        return normalized;
    }

    private static string? NormalizeComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            return null;
        }

        var normalized = comment.Trim();
        if (normalized.Length > 1000)
        {
            throw new ReputationValidationException("Comment cannot exceed 1000 characters.");
        }

        return normalized;
    }

    private static ReviewResponse Map(ReputationReview review)
    {
        return new ReviewResponse(
            review.Id,
            review.TradeProposalId,
            review.ReviewerUserId,
            review.RevieweeUserId,
            review.Rating,
            review.Comment,
            review.CreatedAtUtc,
            review.ReviewerName,
            review.Title);
    }
}
