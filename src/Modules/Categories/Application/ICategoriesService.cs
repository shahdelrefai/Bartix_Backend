using Bartrix.Modules.Categories.Contracts;

namespace Bartrix.Modules.Categories.Application;

public interface ICategoriesService
{
    Task<IReadOnlyList<ApprovedCategoryResponse>> GetApprovedAsync(CancellationToken cancellationToken);
    Task<CategorySuggestionResponse> SuggestAsync(Guid userId, SuggestCategoryRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<CategorySuggestionResponse>> GetSuggestionsAsync(string? status, CancellationToken cancellationToken);
    Task ApproveSuggestionAsync(Guid adminId, string adminName, Guid suggestionId, CancellationToken cancellationToken);
    Task RejectSuggestionAsync(Guid adminId, string adminName, Guid suggestionId, CancellationToken cancellationToken);
    Task DeleteSuggestionAsync(Guid suggestionId, CancellationToken cancellationToken);
}
