using System.Security.Claims;
using Bartrix.Modules.Reports.Application;
using Bartrix.Modules.Reports.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Bartrix.Modules.Reports.Api;

public static class ReportsEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapReportsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports");
        group.WithTags("Reports");
        group.AddEndpointFilter<ReportsValidationFilter>();

        group.MapPost("/", async (
            ClaimsPrincipal principal,
            SubmitReportRequest request,
            IReportsService service,
            CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            var result = await service.SubmitAsync(userId, request, ct);
            return Results.Created($"/api/reports/{result.Id}", result);
        }).RequireAuthorization();

        group.MapGet("/", async (
            IReportsService service,
            CancellationToken ct) =>
        {
            var result = await service.GetAllAsync(ct);
            return Results.Ok(result);
        }).RequireAuthorization("Admin");

        group.MapPut("/{id:guid}/status", async (
            Guid id,
            UpdateReportStatusRequest request,
            IReportsService service,
            CancellationToken ct) =>
        {
            await service.UpdateStatusAsync(id, request, ct);
            return Results.NoContent();
        }).RequireAuthorization("Admin");

        return app;
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (!Guid.TryParse(value, out var userId))
            throw new ReportsValidationException("Authenticated user identifier is invalid.");
        return userId;
    }
}
