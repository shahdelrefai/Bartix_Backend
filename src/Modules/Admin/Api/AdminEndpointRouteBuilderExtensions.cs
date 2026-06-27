using Bartrix.Modules.Admin.Contracts;
using Bartrix.Modules.Admin.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Bartrix.Modules.Admin.Api;

public static class AdminEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin");
        group.WithTags("Admin");
        group.RequireAuthorization("Admin");
        group.AddEndpointFilter<AdminValidationFilter>();

        group.MapGet("/users", async (
            string? search,
            IAdminService service,
            CancellationToken ct) =>
        {
            var result = await service.GetUsersAsync(search, ct);
            return Results.Ok(result);
        });

        group.MapGet("/users/{id:guid}", async (
            Guid id,
            IAdminService service,
            CancellationToken ct) =>
        {
            var result = await service.GetUserAsync(id, ct);
            return Results.Ok(result);
        });

        group.MapPut("/users/{id:guid}/role", async (
            Guid id,
            UpdateUserRoleRequest request,
            IAdminService service,
            CancellationToken ct) =>
        {
            await service.SetRoleAsync(id, request.Role, ct);
            return Results.NoContent();
        });

        group.MapPost("/users/{id:guid}/suspend", async (
            Guid id,
            IAdminService service,
            CancellationToken ct) =>
        {
            await service.SuspendUserAsync(id, ct);
            return Results.NoContent();
        });

        group.MapPost("/users/{id:guid}/unsuspend", async (
            Guid id,
            IAdminService service,
            CancellationToken ct) =>
        {
            await service.UnsuspendUserAsync(id, ct);
            return Results.NoContent();
        });

        group.MapDelete("/users/{id:guid}", async (
            Guid id,
            IAdminService service,
            CancellationToken ct) =>
        {
            await service.DeleteUserDataAsync(id, ct);
            return Results.NoContent();
        });

        group.MapGet("/stats", async (
            IAdminService service,
            CancellationToken ct) =>
        {
            var result = await service.GetStatsAsync(ct);
            return Results.Ok(result);
        });

        group.MapGet("/users/{id:guid}/stats", async (
            Guid id,
            IAdminService service,
            CancellationToken ct) =>
        {
            var result = await service.GetProfileStatsAsync(id, ct);
            return Results.Ok(result);
        });

        // Listing management
        group.MapGet("/listings", async (
            string? search, string? category, string? status,
            int page, int pageSize,
            IAdminService adminService,
            CancellationToken cancellationToken) =>
        {
            var p = page <= 0 ? 1 : page;
            var ps = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
            var listings = await adminService.GetListingsAsync(search, category, status, p, ps, cancellationToken);
            return Results.Ok(listings);
        });

        group.MapDelete("/listings/{id:guid}", async (
            Guid id,
            IAdminService adminService,
            CancellationToken cancellationToken) =>
        {
            await adminService.SoftDeleteListingAsync(id, cancellationToken);
            return Results.NoContent();
        });

        group.MapPut("/listings/{id:guid}/status", async (
            Guid id,
            AdminUpdateListingStatusRequest request,
            IAdminService adminService,
            CancellationToken cancellationToken) =>
        {
            await adminService.UpdateListingStatusAsync(id, request.Status, cancellationToken);
            return Results.NoContent();
        });

        return app;
    }
}
