using System.Security.Claims;
using Bartrix.Modules.Withdrawals.Application;
using Bartrix.Modules.Withdrawals.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Bartrix.Modules.Withdrawals.Api;

public static class WithdrawalsEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapWithdrawalsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/withdrawals");
        group.WithTags("Withdrawals");
        group.RequireAuthorization();
        group.AddEndpointFilter<WithdrawalsValidationFilter>();

        // Seller: create a withdrawal request
        group.MapPost("/", async (
            ClaimsPrincipal principal,
            CreateWithdrawalRequest request,
            IWithdrawalService withdrawalService,
            CancellationToken cancellationToken) =>
        {
            var sellerId = GetUserId(principal);
            var response = await withdrawalService.CreateAsync(sellerId, request, cancellationToken);
            return Results.Created($"/api/withdrawals/{response.Id}", response);
        });

        // Seller: list own requests
        group.MapGet("/", async (
            ClaimsPrincipal principal,
            IWithdrawalService withdrawalService,
            CancellationToken cancellationToken) =>
        {
            var sellerId = GetUserId(principal);
            var result = await withdrawalService.GetForSellerAsync(sellerId, cancellationToken);
            return Results.Ok(result);
        });

        // Admin: list all requests
        group.MapGet("/all", async (
            IWithdrawalService withdrawalService,
            CancellationToken cancellationToken) =>
        {
            var result = await withdrawalService.GetAllAsync(cancellationToken);
            return Results.Ok(result);
        }).RequireAuthorization("Admin");

        // Admin: update status (completed or rejected)
        group.MapPatch("/{requestId:guid}/status", async (
            Guid requestId,
            UpdateWithdrawalStatusRequest request,
            IWithdrawalService withdrawalService,
            CancellationToken cancellationToken) =>
        {
            var response = await withdrawalService.UpdateStatusAsync(requestId, request, cancellationToken);
            return Results.Ok(response);
        }).RequireAuthorization("Admin");

        return app;
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");

        if (!Guid.TryParse(value, out var userId))
        {
            throw new WithdrawalValidationException("Authenticated user identifier is invalid.");
        }

        return userId;
    }
}
