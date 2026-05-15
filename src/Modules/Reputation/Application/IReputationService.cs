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
}
