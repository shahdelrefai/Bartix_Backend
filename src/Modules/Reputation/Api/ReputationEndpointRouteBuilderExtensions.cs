using System.Security.Claims;
using Bartrix.Modules.Reputation.Application;
using Bartrix.Modules.Reputation.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Bartrix.Modules.Reputation.Api;

public static class ReputationEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapReputationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reputation");
        group.WithTags("Reputation");
        group.AddEndpointFilter<ReputationValidationFilter>();

        group.MapGet("/users/{userId:guid}", async (
            Guid userId,
            IReputationService reputationService,
            CancellationToken cancellationToken) =>
        {
            var response = await reputationService.GetUserReputationAsync(userId, cancellationToken);
            return Results.Ok(response);
        });

        group.MapPost("/trades/{tradeProposalId:guid}", async (
            ClaimsPrincipal principal,
            Guid tradeProposalId,
            CreateReviewRequest request,
            IReputationService reputationService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var response = await reputationService.CreateTradeReviewAsync(userId, tradeProposalId, request, cancellationToken);
            return Results.Ok(response);
        }).RequireAuthorization();

        return app;
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");

        if (!Guid.TryParse(value, out var userId))
        {
            throw new ReputationValidationException("Authenticated user identifier is invalid.");
        }

        return userId;
    }
}
