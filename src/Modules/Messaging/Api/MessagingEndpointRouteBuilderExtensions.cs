using System.Security.Claims;
using Bartrix.Modules.Messaging.Application;
using Bartrix.Modules.Messaging.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Bartrix.Modules.Messaging.Api;

public static class MessagingEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapMessagingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/messages");
        group.WithTags("Messaging");
        group.RequireAuthorization();
        group.AddEndpointFilter<MessagingValidationFilter>();

        group.MapGet("/conversations", async (
            ClaimsPrincipal principal,
            IMessagingService messagingService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var response = await messagingService.GetMyConversationsAsync(userId, cancellationToken);
            return Results.Ok(response);
        });

        group.MapGet("/trades/{tradeProposalId:guid}", async (
            ClaimsPrincipal principal,
            Guid tradeProposalId,
            IMessagingService messagingService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var response = await messagingService.GetTradeConversationAsync(userId, tradeProposalId, cancellationToken);
            return Results.Ok(response);
        });

        group.MapPost("/trades/{tradeProposalId:guid}", async (
            ClaimsPrincipal principal,
            Guid tradeProposalId,
            SendMessageRequest request,
            IMessagingService messagingService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var response = await messagingService.SendTradeMessageAsync(userId, tradeProposalId, request, cancellationToken);
            return Results.Ok(response);
        });

        // ─── Direct (non-trade) conversations ───────────────────────────
        group.MapPost("/conversations/with/{otherUserId:guid}", async (
            ClaimsPrincipal principal,
            Guid otherUserId,
            IMessagingService messagingService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var response = await messagingService.GetOrCreateDirectConversationAsync(userId, otherUserId, cancellationToken);
            return Results.Ok(response);
        });

        group.MapGet("/conversations/{conversationId:guid}", async (
            ClaimsPrincipal principal,
            Guid conversationId,
            IMessagingService messagingService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var response = await messagingService.GetConversationAsync(userId, conversationId, cancellationToken);
            return Results.Ok(response);
        });

        group.MapPost("/conversations/{conversationId:guid}", async (
            ClaimsPrincipal principal,
            Guid conversationId,
            SendMessageRequest request,
            IMessagingService messagingService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var response = await messagingService.SendConversationMessageAsync(userId, conversationId, request, cancellationToken);
            return Results.Ok(response);
        });

        group.MapPost("/conversations/{conversationId:guid}/read", async (
            ClaimsPrincipal principal,
            Guid conversationId,
            IMessagingService messagingService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            await messagingService.MarkConversationReadAsync(userId, conversationId, cancellationToken);
            return Results.NoContent();
        });

        return app;
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");

        if (!Guid.TryParse(value, out var userId))
        {
            throw new MessagingValidationException("Authenticated user identifier is invalid.");
        }

        return userId;
    }
}
