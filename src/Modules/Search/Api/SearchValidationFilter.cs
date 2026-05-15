using Bartrix.Modules.Search.Application;
using Microsoft.AspNetCore.Http;

namespace Bartrix.Modules.Search.Api;

public sealed class SearchValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (SearchValidationException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["search"] = new[] { exception.Message }
            });
        }
    }
}
