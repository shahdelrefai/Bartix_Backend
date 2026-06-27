namespace Bartrix.Modules.Categories.Domain;

public sealed class ApprovedCategory
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Guid AddedBy { get; private set; }
    public string AddedByName { get; private set; } = string.Empty;
    public DateTimeOffset AddedAtUtc { get; private set; }

    public ApprovedCategory(string name, Guid addedBy, string addedByName, DateTimeOffset addedAtUtc)
    {
        Id = Guid.NewGuid();
        Name = name.Trim();
        AddedBy = addedBy;
        AddedByName = addedByName;
        AddedAtUtc = addedAtUtc;
    }

    private ApprovedCategory() { }
}
