using System.Security.Claims;
using Bartrix.Modules.Services.Application;
using Bartrix.Modules.Services.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Bartrix.Modules.Services.Api;

public static class ServicesEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapServicesEndpoints(this IEndpointRouteBuilder app)
    {
        var publicGroup = app.MapGroup("/api/services");
        publicGroup.WithTags("Services");
        publicGroup.AddEndpointFilter<ServicesValidationFilter>();

        publicGroup.MapGet("/", async (
            [AsParameters] ServicesQuery query,
            IServicesService servicesService,
            CancellationToken cancellationToken) =>
        {
            var response = await servicesService.BrowseAsync(query, cancellationToken);
            return Results.Ok(response);
        });

        publicGroup.MapGet("/{serviceOfferId:guid}", async (
            Guid serviceOfferId,
            IServicesService servicesService,
            CancellationToken cancellationToken) =>
        {
            var serviceOffer = await servicesService.GetByIdAsync(serviceOfferId, cancellationToken);
            return serviceOffer is null ? Results.NotFound() : Results.Ok(serviceOffer);
        });

        var authGroup = publicGroup.MapGroup(string.Empty);
        authGroup.RequireAuthorization();
        authGroup.AddEndpointFilter<ServicesValidationFilter>();

        authGroup.MapGet("/mine", async (
            ClaimsPrincipal principal,
            IServicesService servicesService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var items = await servicesService.GetMineAsync(userId, cancellationToken);
            return Results.Ok(items);
        });

        authGroup.MapPost("/", async (
            ClaimsPrincipal principal,
            CreateServiceOfferRequest request,
            IServicesService servicesService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var serviceOffer = await servicesService.CreateAsync(userId, request, cancellationToken);
            return Results.Created($"/api/services/{serviceOffer.Id}", serviceOffer);
        });

        authGroup.MapPut("/{serviceOfferId:guid}", async (
            ClaimsPrincipal principal,
            Guid serviceOfferId,
            UpdateServiceOfferRequest request,
            IServicesService servicesService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var serviceOffer = await servicesService.UpdateAsync(userId, serviceOfferId, request, cancellationToken);
            return Results.Ok(serviceOffer);
        });

        authGroup.MapDelete("/{serviceOfferId:guid}", async (
            ClaimsPrincipal principal,
            Guid serviceOfferId,
            IServicesService servicesService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            await servicesService.ArchiveAsync(userId, serviceOfferId, cancellationToken);
            return Results.NoContent();
        });

        return app;
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");

        if (!Guid.TryParse(value, out var userId))
        {
            throw new ServicesValidationException("Authenticated user identifier is invalid.");
        }

        return userId;
    }
}
