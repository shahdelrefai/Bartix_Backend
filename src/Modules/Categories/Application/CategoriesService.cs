using Bartrix.Modules.Categories.Contracts;
using Bartrix.Modules.Categories.Domain;
using Bartrix.Modules.Categories.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Categories.Application;

public sealed class CategoriesService : ICategoriesService
{
    private readonly CategoriesDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public CategoriesService(CategoriesDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<ApprovedCategoryResponse>> GetApprovedAsync(CancellationToken cancellationToken)
    {
        var categories = await _dbContext.ApprovedCategories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return categories.Select(MapApproved).ToList();
    }

    public async Task<CategorySuggestionResponse> SuggestAsync(Guid userId, SuggestCategoryRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new CategoriesValidationException("Category name is required.");

        var alreadyApproved = await _dbContext.ApprovedCategories
            .AnyAsync(x => x.Name.ToLower() == name.ToLower(), cancellationToken);
        if (alreadyApproved)
            throw new CategoriesValidationException("This category already exists.");

        var pendingDuplicate = await _dbContext.CategorySuggestions
            .AnyAsync(x => x.Status == CategorySuggestion.Statuses.Pending
                           && x.SuggestedName.ToLower() == name.ToLower(), cancellationToken);
        if (pendingDuplicate)
            throw new CategoriesValidationException("This category has already been suggested and is pending approval.");

        var suggestion = new CategorySuggestion(name, userId, request.SuggestedByName, _timeProvider.GetUtcNow());
        _dbContext.CategorySuggestions.Add(suggestion);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapSuggestion(suggestion);
    }

    public async Task<IReadOnlyList<CategorySuggestionResponse>> GetSuggestionsAsync(string? status, CancellationToken cancellationToken)
    {
        var query = _dbContext.CategorySuggestions.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status);

        var items = await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
        return items.Select(MapSuggestion).ToList();
    }

    public async Task ApproveSuggestionAsync(Guid adminId, string adminName, Guid suggestionId, CancellationToken cancellationToken)
    {
        var suggestion = await _dbContext.CategorySuggestions
            .SingleOrDefaultAsync(x => x.Id == suggestionId, cancellationToken)
            ?? throw new CategoriesValidationException("Suggestion not found.");

        if (suggestion.Status != CategorySuggestion.Statuses.Pending)
            throw new CategoriesValidationException("Only pending suggestions can be approved.");

        suggestion.Approve(adminId, adminName, _timeProvider.GetUtcNow());

        var approved = new ApprovedCategory(suggestion.SuggestedName, adminId, adminName, _timeProvider.GetUtcNow());
        _dbContext.ApprovedCategories.Add(approved);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectSuggestionAsync(Guid adminId, string adminName, Guid suggestionId, CancellationToken cancellationToken)
    {
        var suggestion = await _dbContext.CategorySuggestions
            .SingleOrDefaultAsync(x => x.Id == suggestionId, cancellationToken)
            ?? throw new CategoriesValidationException("Suggestion not found.");

        if (suggestion.Status != CategorySuggestion.Statuses.Pending)
            throw new CategoriesValidationException("Only pending suggestions can be rejected.");

        suggestion.Reject(adminId, adminName, _timeProvider.GetUtcNow());
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteSuggestionAsync(Guid suggestionId, CancellationToken cancellationToken)
    {
        var suggestion = await _dbContext.CategorySuggestions
            .SingleOrDefaultAsync(x => x.Id == suggestionId, cancellationToken)
            ?? throw new CategoriesValidationException("Suggestion not found.");

        _dbContext.CategorySuggestions.Remove(suggestion);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ApprovedCategoryResponse MapApproved(ApprovedCategory c) =>
        new(c.Id, c.Name, c.AddedBy, c.AddedByName, c.AddedAtUtc);

    private static CategorySuggestionResponse MapSuggestion(CategorySuggestion s) =>
        new(s.Id, s.SuggestedName, s.SuggestedBy, s.SuggestedByName, s.Status,
            s.CreatedAtUtc, s.ReviewedBy, s.ReviewedByName, s.ReviewedAtUtc);
}
