using Bartrix.Modules.Payments.Application;
using Microsoft.AspNetCore.Http;

namespace Bartrix.Modules.Payments.Api;

internal sealed class PaymentsValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        try
        {
            return await next(ctx);
        }
        catch (PaymentValidationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
