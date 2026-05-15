using System.Security.Claims;
using Bartrix.Modules.Trades.Application;
using Bartrix.Modules.Trades.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Bartrix.Modules.Trades.Api;

public static class TradesEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapTradesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/trades");
        group.WithTags("Trades");
        group.RequireAuthorization();
        group.AddEndpointFilter<TradesValidationFilter>();

        group.MapGet("/mine", async (
            ClaimsPrincipal principal,
            ITradesService tradesService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var response = await tradesService.GetMyTradesAsync(userId, cancellationToken);
            return Results.Ok(response);
        });

        group.MapGet("/{tradeId:guid}", async (
            ClaimsPrincipal principal,
            Guid tradeId,
            ITradesService tradesService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var response = await tradesService.GetByIdAsync(userId, tradeId, cancellationToken);
            return response is null ? Results.NotFound() : Results.Ok(response);
        });

        group.MapPost("/", async (
            ClaimsPrincipal principal,
            CreateTradeProposalRequest request,
            ITradesService tradesService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var response = await tradesService.CreateAsync(userId, request, cancellationToken);
            return Results.Created($"/api/trades/{response.Id}", response);
        });

        group.MapPost("/{tradeId:guid}/accept", async (
            ClaimsPrincipal principal,
            Guid tradeId,
            ITradesService tradesService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var response = await tradesService.AcceptAsync(userId, tradeId, cancellationToken);
            return Results.Ok(response);
        });

        group.MapPost("/{tradeId:guid}/reject", async (
            ClaimsPrincipal principal,
            Guid tradeId,
            ITradesService tradesService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var response = await tradesService.RejectAsync(userId, tradeId, cancellationToken);
            return Results.Ok(response);
        });

        group.MapPost("/{tradeId:guid}/cancel", async (
            ClaimsPrincipal principal,
            Guid tradeId,
            ITradesService tradesService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            await tradesService.CancelAsync(userId, tradeId, cancellationToken);
            return Results.NoContent();
        });

        return app;
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");

        if (!Guid.TryParse(value, out var userId))
        {
            throw new TradesValidationException("Authenticated user identifier is invalid.");
        }

        return userId;
    }
}
