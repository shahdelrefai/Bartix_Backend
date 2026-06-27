using System.Security.Claims;
using Bartrix.Modules.Listings.Application;
using Bartrix.Modules.Listings.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Bartrix.Modules.Listings.Api;

public static class ListingsEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapListingsEndpoints(this IEndpointRouteBuilder app)
    {
        var publicGroup = app.MapGroup("/api/listings");
        publicGroup.WithTags("Listings");
        publicGroup.AddEndpointFilter<ListingsValidationFilter>();

        publicGroup.MapGet("/", async (
            [AsParameters] ListingsQuery query,
            IListingsService listingsService,
            CancellationToken cancellationToken) =>
        {
            var response = await listingsService.BrowseAsync(query, cancellationToken);
            return Results.Ok(response);
        });

        publicGroup.MapPost("/by-ids", async (
            IReadOnlyList<Guid> ids,
            IListingsService listingsService,
            CancellationToken cancellationToken) =>
        {
            var items = await listingsService.GetByIdsAsync(ids ?? Array.Empty<Guid>(), cancellationToken);
            return Results.Ok(items);
        });

        publicGroup.MapGet("/{listingId:guid}", async (
            Guid listingId,
            IListingsService listingsService,
            CancellationToken cancellationToken) =>
        {
            var listing = await listingsService.GetByIdAsync(listingId, cancellationToken);
            return listing is null ? Results.NotFound() : Results.Ok(listing);
        });

        var authGroup = publicGroup.MapGroup(string.Empty);
        authGroup.RequireAuthorization();
        authGroup.AddEndpointFilter<ListingsValidationFilter>();

        authGroup.MapGet("/mine", async (
            ClaimsPrincipal principal,
            IListingsService listingsService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var items = await listingsService.GetMineAsync(userId, cancellationToken);
            return Results.Ok(items);
        });

        authGroup.MapPost("/", async (
            ClaimsPrincipal principal,
            CreateListingRequest request,
            IListingsService listingsService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var listing = await listingsService.CreateAsync(userId, request, cancellationToken);
            return Results.Created($"/api/listings/{listing.Id}", listing);
        });

        authGroup.MapPut("/{listingId:guid}", async (
            ClaimsPrincipal principal,
            Guid listingId,
            UpdateListingRequest request,
            IListingsService listingsService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var listing = await listingsService.UpdateAsync(userId, listingId, request, cancellationToken);
            return Results.Ok(listing);
        });

        authGroup.MapDelete("/{listingId:guid}", async (
            ClaimsPrincipal principal,
            Guid listingId,
            IListingsService listingsService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            await listingsService.ArchiveAsync(userId, listingId, cancellationToken);
            return Results.NoContent();
        });

        authGroup.MapPut("/{listingId:guid}/status", async (
            ClaimsPrincipal principal,
            Guid listingId,
            UpdateListingStatusRequest request,
            IListingsService listingsService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var listing = await listingsService.UpdateStatusAsync(userId, listingId, request, cancellationToken);
            return Results.Ok(listing);
        });

        // ─── Favourites ─────────────────────────────────────────────────
        authGroup.MapGet("/favourites", async (
            ClaimsPrincipal principal,
            IListingsService listingsService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var items = await listingsService.GetFavouritesAsync(userId, cancellationToken);
            return Results.Ok(items);
        });

        authGroup.MapPost("/{listingId:guid}/favourite", async (
            ClaimsPrincipal principal,
            Guid listingId,
            IListingsService listingsService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var state = await listingsService.ToggleFavouriteAsync(userId, listingId, cancellationToken);
            return Results.Ok(state);
        });

        authGroup.MapPut("/{listingId:guid}/favourite", async (
            ClaimsPrincipal principal,
            Guid listingId,
            IListingsService listingsService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            await listingsService.AddFavouriteAsync(userId, listingId, cancellationToken);
            return Results.NoContent();
        });

        authGroup.MapDelete("/{listingId:guid}/favourite", async (
            ClaimsPrincipal principal,
            Guid listingId,
            IListingsService listingsService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            await listingsService.RemoveFavouriteAsync(userId, listingId, cancellationToken);
            return Results.NoContent();
        });

        authGroup.MapGet("/{listingId:guid}/favourite", async (
            ClaimsPrincipal principal,
            Guid listingId,
            IListingsService listingsService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var isFavourite = await listingsService.IsFavouriteAsync(userId, listingId, cancellationToken);
            return Results.Ok(new FavouriteStateResponse(listingId, isFavourite));
        });

        // ─── Views & reports ────────────────────────────────────────────
        authGroup.MapPost("/{listingId:guid}/view", async (
            ClaimsPrincipal principal,
            Guid listingId,
            IListingsService listingsService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            await listingsService.IncrementViewAsync(listingId, userId, cancellationToken);
            return Results.NoContent();
        });

        authGroup.MapPost("/ai-suggest", async (
            AiSuggestRequest request,
            IAiDescriptionService aiService,
            CancellationToken cancellationToken) =>
        {
            var suggestions = await aiService.GetSuggestionsAsync(request.ImageUrl, request.Category, request.Condition, cancellationToken);
            return Results.Ok(new AiSuggestResponse(suggestions));
        }).RequireAuthorization();

        authGroup.MapPost("/{listingId:guid}/report", async (
            ClaimsPrincipal principal,
            Guid listingId,
            IListingsService listingsService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            await listingsService.ReportAsync(listingId, userId, cancellationToken);
            return Results.NoContent();
        });

        return app;
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");

        if (!Guid.TryParse(value, out var userId))
        {
            throw new ListingsValidationException("Authenticated user identifier is invalid.");
        }

        return userId;
    }
}
