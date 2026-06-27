using Bartrix.Modules.Wallet.Application;
using Microsoft.AspNetCore.Http;

namespace Bartrix.Modules.Wallet.Api;

internal sealed class WalletValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        try
        {
            return await next(ctx);
        }
        catch (WalletValidationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
