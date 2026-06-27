using System.Security.Claims;
using Bartrix.Modules.Wallet.Application;
using Bartrix.Modules.Wallet.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Bartrix.Modules.Wallet.Api;

public static class WalletEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapWalletEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/wallet");
        group.WithTags("Wallet");
        group.RequireAuthorization();
        group.AddEndpointFilter<WalletValidationFilter>();

        group.MapGet("/balance", async (
            ClaimsPrincipal principal,
            WalletService walletService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var balance = await walletService.GetBalanceAsync(userId, cancellationToken);
            return Results.Ok(new WalletBalanceResponse(balance));
        });

        group.MapGet("/transactions", async (
            ClaimsPrincipal principal,
            WalletService walletService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var transactions = await walletService.GetTransactionsAsync(userId, cancellationToken);
            return Results.Ok(transactions);
        });

        return app;
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");

        if (!Guid.TryParse(value, out var userId))
        {
            throw new WalletValidationException("Authenticated user identifier is invalid.");
        }

        return userId;
    }
}
