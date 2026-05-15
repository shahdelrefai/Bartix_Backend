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
