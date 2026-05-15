using Bartrix.Modules.Trades.Application;
using Microsoft.AspNetCore.Http;

namespace Bartrix.Modules.Trades.Api;

public sealed class TradesValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (TradesValidationException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["trade"] = new[] { exception.Message }
            });
        }
    }
}
