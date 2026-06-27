namespace Bartrix.Modules.Categories.Domain;

public sealed class CategorySuggestion
{
    public Guid Id { get; private set; }
    public string SuggestedName { get; private set; } = string.Empty;
    public Guid SuggestedBy { get; private set; }
    public string SuggestedByName { get; private set; } = string.Empty;
    public string Status { get; private set; } = Statuses.Pending;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public string? ReviewedByName { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }

    public static class Statuses
    {
        public const string Pending = "pending";
        public const string Approved = "approved";
        public const string Rejected = "rejected";
    }

    public CategorySuggestion(string name, Guid suggestedBy, string suggestedByName, DateTimeOffset createdAtUtc)
    {
        Id = Guid.NewGuid();
        SuggestedName = name.Trim();
        SuggestedBy = suggestedBy;
        SuggestedByName = suggestedByName;
        CreatedAtUtc = createdAtUtc;
    }

    public void Approve(Guid adminId, string adminName, DateTimeOffset reviewedAt)
    {
        Status = Statuses.Approved;
        ReviewedBy = adminId;
        ReviewedByName = adminName;
        ReviewedAtUtc = reviewedAt;
    }

    public void Reject(Guid adminId, string adminName, DateTimeOffset reviewedAt)
    {
        Status = Statuses.Rejected;
        ReviewedBy = adminId;
        ReviewedByName = adminName;
        ReviewedAtUtc = reviewedAt;
    }

    private CategorySuggestion() { }
}
