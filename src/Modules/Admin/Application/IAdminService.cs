using Bartrix.Modules.Admin.Contracts;

namespace Bartrix.Modules.Admin.Application;

public interface IAdminService
{
    Task<IReadOnlyList<AdminUserResponse>> GetUsersAsync(string? search, CancellationToken cancellationToken);
    Task<AdminUserResponse> GetUserAsync(Guid userId, CancellationToken cancellationToken);
    Task SetRoleAsync(Guid userId, string role, CancellationToken cancellationToken);
    Task SuspendUserAsync(Guid userId, CancellationToken cancellationToken);
    Task UnsuspendUserAsync(Guid userId, CancellationToken cancellationToken);
    Task DeleteUserDataAsync(Guid userId, CancellationToken cancellationToken);
    Task<AdminStatsResponse> GetStatsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AdminListingResponse>> GetListingsAsync(string? search, string? category, string? status, int page, int pageSize, CancellationToken cancellationToken);
    Task SoftDeleteListingAsync(Guid listingId, CancellationToken cancellationToken);
    Task UpdateListingStatusAsync(Guid listingId, string status, CancellationToken cancellationToken);
    Task<ProfileStatsResponse> GetProfileStatsAsync(Guid userId, CancellationToken cancellationToken);
}
