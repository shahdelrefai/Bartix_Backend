using System.Security.Claims;
using Bartrix.Modules.Profiles.Application;
using Bartrix.Modules.Profiles.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Bartrix.Modules.Profiles.Api;

public static class ProfileEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profile");
        group.WithTags("Profiles");
        group.RequireAuthorization();
        group.AddEndpointFilter<ProfileValidationFilter>();

        group.MapGet("/me", async (
            ClaimsPrincipal principal,
            IProfileService profileService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var displayName = principal.FindFirstValue(ClaimTypes.Name);
            var response = await profileService.GetMyProfileAsync(userId, displayName, cancellationToken);
            return Results.Ok(response);
        });

        group.MapPut("/me", async (
            ClaimsPrincipal principal,
            UpdateMyProfileRequest request,
            IProfileService profileService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var displayName = principal.FindFirstValue(ClaimTypes.Name);
            var response = await profileService.UpdateMyProfileAsync(userId, displayName, request, cancellationToken);
            return Results.Ok(response);
        });

        return app;
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");

        if (!Guid.TryParse(value, out var userId))
        {
            throw new ProfileValidationException("Authenticated user identifier is invalid.");
        }

        return userId;
    }
}
