using Bartrix.Modules.Delivery.Application;
using Microsoft.AspNetCore.Http;

namespace Bartrix.Modules.Delivery.Api;

public sealed class DeliveryValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (DeliveryValidationException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["delivery"] = new[] { exception.Message }
            });
        }
    }
}
