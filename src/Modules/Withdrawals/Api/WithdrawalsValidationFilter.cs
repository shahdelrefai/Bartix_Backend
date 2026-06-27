using Bartrix.Modules.Withdrawals.Application;
using Microsoft.AspNetCore.Http;

namespace Bartrix.Modules.Withdrawals.Api;

internal sealed class WithdrawalsValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        try
        {
            return await next(ctx);
        }
        catch (WithdrawalValidationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
