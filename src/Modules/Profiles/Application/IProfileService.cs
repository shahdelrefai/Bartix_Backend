using Bartrix.Modules.Profiles.Contracts;

namespace Bartrix.Modules.Profiles.Application;

public interface IProfileService
{
    Task<ProfileResponse> GetMyProfileAsync(
        Guid userId,
        string? claimedDisplayName,
        CancellationToken cancellationToken);

    Task<ProfileResponse> UpdateMyProfileAsync(
        Guid userId,
        string? claimedDisplayName,
        UpdateMyProfileRequest request,
        CancellationToken cancellationToken);
}
