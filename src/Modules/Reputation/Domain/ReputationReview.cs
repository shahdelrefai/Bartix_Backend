namespace Bartrix.Modules.Reputation.Domain;

public sealed class ReputationReview
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid TradeProposalId { get; private set; }

    public Guid ReviewerUserId { get; private set; }

    public Guid RevieweeUserId { get; private set; }

    public int Rating { get; private set; }

    public string? Comment { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private ReputationReview()
    {
    }

    public ReputationReview(
        Guid tradeProposalId,
        Guid reviewerUserId,
        Guid revieweeUserId,
        int rating,
        string? comment,
        DateTimeOffset createdAtUtc)
    {
        TradeProposalId = tradeProposalId;
        ReviewerUserId = reviewerUserId;
        RevieweeUserId = revieweeUserId;
        Rating = rating;
        Comment = comment;
        CreatedAtUtc = createdAtUtc;
    }
}
