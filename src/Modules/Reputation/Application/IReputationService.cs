using Bartrix.Modules.Reputation.Contracts;

namespace Bartrix.Modules.Reputation.Application;

public interface IReputationService
{
    Task<ReviewResponse> CreateTradeReviewAsync(
        Guid reviewerUserId,
        Guid tradeProposalId,
        CreateReviewRequest request,
        CancellationToken cancellationToken);

    Task<ReputationSummaryResponse> GetUserReputationAsync(Guid userId, CancellationToken cancellationToken);

    // ─── Direct user-to-user reviews ────────────────────────────────────
    Task<ReviewResponse> CreateUserReviewAsync(
        Guid reviewerUserId,
        Guid targetUserId,
        CreateUserReviewRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ReviewResponse>> GetUserReviewsAsync(Guid userId, CancellationToken cancellationToken);
}
