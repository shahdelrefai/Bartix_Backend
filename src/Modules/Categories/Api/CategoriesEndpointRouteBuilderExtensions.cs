using System.Security.Claims;
using Bartrix.Modules.Categories.Application;
using Bartrix.Modules.Categories.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Bartrix.Modules.Categories.Api;

public static class CategoriesEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapCategoriesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/categories");
        group.WithTags("Categories");
        group.AddEndpointFilter<CategoriesValidationFilter>();

        group.MapGet("/", async (
            ICategoriesService service,
            CancellationToken ct) =>
        {
            var result = await service.GetApprovedAsync(ct);
            return Results.Ok(result);
        });

        group.MapPost("/suggest", async (
            ClaimsPrincipal principal,
            SuggestCategoryRequest request,
            ICategoriesService service,
            CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            var result = await service.SuggestAsync(userId, request, ct);
            return Results.Created($"/api/categories/suggestions/{result.Id}", result);
        }).RequireAuthorization();

        var admin = app.MapGroup("/api/categories/suggestions");
        admin.WithTags("Categories");
        admin.RequireAuthorization("Admin");
        admin.AddEndpointFilter<CategoriesValidationFilter>();

        admin.MapGet("/", async (
            string? status,
            ICategoriesService service,
            CancellationToken ct) =>
        {
            var result = await service.GetSuggestionsAsync(status, ct);
            return Results.Ok(result);
        });

        admin.MapPost("/{id:guid}/approve", async (
            ClaimsPrincipal principal,
            Guid id,
            ICategoriesService service,
            CancellationToken ct) =>
        {
            var adminId = GetUserId(principal);
            var adminName = principal.FindFirstValue(ClaimTypes.Name) ?? "Admin";
            await service.ApproveSuggestionAsync(adminId, adminName, id, ct);
            return Results.NoContent();
        });

        admin.MapPost("/{id:guid}/reject", async (
            ClaimsPrincipal principal,
            Guid id,
            ICategoriesService service,
            CancellationToken ct) =>
        {
            var adminId = GetUserId(principal);
            var adminName = principal.FindFirstValue(ClaimTypes.Name) ?? "Admin";
            await service.RejectSuggestionAsync(adminId, adminName, id, ct);
            return Results.NoContent();
        });

        admin.MapDelete("/{id:guid}", async (
            Guid id,
            ICategoriesService service,
            CancellationToken ct) =>
        {
            await service.DeleteSuggestionAsync(id, ct);
            return Results.NoContent();
        });

        return app;
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (!Guid.TryParse(value, out var userId))
            throw new CategoriesValidationException("Authenticated user identifier is invalid.");
        return userId;
    }
}
