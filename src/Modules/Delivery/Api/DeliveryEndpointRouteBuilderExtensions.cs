using System.Security.Claims;
using Bartrix.Modules.Delivery.Application;
using Bartrix.Modules.Delivery.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Bartrix.Modules.Delivery.Api;

public static class DeliveryEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapDeliveryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/delivery");
        group.WithTags("Delivery");
        group.RequireAuthorization();
        group.AddEndpointFilter<DeliveryValidationFilter>();

        group.MapGet("/trades/{tradeProposalId:guid}", async (
            ClaimsPrincipal principal,
            Guid tradeProposalId,
            IDeliveryService deliveryService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var response = await deliveryService.GetTradeDeliveryAsync(userId, tradeProposalId, cancellationToken);
            return Results.Ok(response);
        });

        group.MapPut("/trades/{tradeProposalId:guid}", async (
            ClaimsPrincipal principal,
            Guid tradeProposalId,
            UpdateDeliveryRequest request,
            IDeliveryService deliveryService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var response = await deliveryService.UpdateTradeDeliveryAsync(userId, tradeProposalId, request, cancellationToken);
            return Results.Ok(response);
        });

        group.MapPost("/trades/{tradeProposalId:guid}/delivered", async (
            ClaimsPrincipal principal,
            Guid tradeProposalId,
            IDeliveryService deliveryService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var response = await deliveryService.MarkDeliveredAsync(userId, tradeProposalId, cancellationToken);
            return Results.Ok(response);
        });

        group.MapPost("/trades/{tradeProposalId:guid}/confirm", async (
            ClaimsPrincipal principal,
            Guid tradeProposalId,
            IDeliveryService deliveryService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var response = await deliveryService.ConfirmAsync(userId, tradeProposalId, cancellationToken);
            return Results.Ok(response);
        });

        group.MapPost("/trades/{tradeProposalId:guid}/cancel", async (
            ClaimsPrincipal principal,
            Guid tradeProposalId,
            IDeliveryService deliveryService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var response = await deliveryService.CancelAsync(userId, tradeProposalId, cancellationToken);
            return Results.Ok(response);
        });

        return app;
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");

        if (!Guid.TryParse(value, out var userId))
        {
            throw new DeliveryValidationException("Authenticated user identifier is invalid.");
        }

        return userId;
    }
}
