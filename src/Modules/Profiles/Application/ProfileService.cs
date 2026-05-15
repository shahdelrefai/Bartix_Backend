using Bartrix.Modules.Profiles.Contracts;
using Bartrix.Modules.Profiles.Domain;
using Bartrix.Modules.Profiles.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Profiles.Application;

public sealed class ProfileService : IProfileService
{
    private readonly ProfilesDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public ProfileService(ProfilesDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<ProfileResponse> GetMyProfileAsync(
        Guid userId,
        string? claimedDisplayName,
        CancellationToken cancellationToken)
    {
        var profile = await _dbContext.UserProfiles.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (profile is null)
        {
            profile = await CreateDefaultProfileAsync(userId, claimedDisplayName, cancellationToken);
        }

        return Map(profile);
    }

    public async Task<ProfileResponse> UpdateMyProfileAsync(
        Guid userId,
        string? claimedDisplayName,
        UpdateMyProfileRequest request,
        CancellationToken cancellationToken)
    {
        var displayName = NormalizeDisplayName(request.DisplayName ?? claimedDisplayName);
        var bio = NormalizeOptionalText(request.Bio, 1000);
        var location = NormalizeOptionalText(request.Location, 200);
        var avatarUrl = NormalizeOptionalText(request.AvatarUrl, 500);
        var nowUtc = _timeProvider.GetUtcNow();

        var profile = await _dbContext.UserProfiles.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (profile is null)
        {
            profile = new UserProfile(
                userId,
                displayName,
                bio,
                location,
                avatarUrl,
                nowUtc,
                nowUtc);

            _dbContext.UserProfiles.Add(profile);
        }
        else
        {
            profile.Update(displayName, bio, location, avatarUrl, nowUtc);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(profile);
    }

    private async Task<UserProfile> CreateDefaultProfileAsync(
        Guid userId,
        string? claimedDisplayName,
        CancellationToken cancellationToken)
    {
        var nowUtc = _timeProvider.GetUtcNow();
        var profile = new UserProfile(
            userId,
            NormalizeDisplayName(claimedDisplayName),
            null,
            null,
            null,
            nowUtc,
            nowUtc);

        _dbContext.UserProfiles.Add(profile);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return profile;
    }

    private static ProfileResponse Map(UserProfile profile)
    {
        return new ProfileResponse(
            profile.UserId,
            profile.DisplayName,
            profile.Bio,
            profile.Location,
            profile.AvatarUrl,
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc);
    }

    private static string NormalizeDisplayName(string? displayName)
    {
        var normalized = (displayName ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = "Bartrix User";
        }

        if (normalized.Length > 200)
        {
            throw new ProfileValidationException("Display name cannot exceed 200 characters.");
        }

        return normalized;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ProfileValidationException($"Field length cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}
