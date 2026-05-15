using Bartrix.Modules.Listings.Application;
using Microsoft.AspNetCore.Http;

namespace Bartrix.Modules.Listings.Api;

public sealed class ListingsValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (ListingsValidationException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["listing"] = new[] { exception.Message }
            });
        }
    }
}
