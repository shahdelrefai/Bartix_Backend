using Bartrix.Modules.Categories.Application;
using Microsoft.AspNetCore.Http;

namespace Bartrix.Modules.Categories.Api;

public sealed class CategoriesValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (CategoriesValidationException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["category"] = new[] { ex.Message }
            });
        }
    }
}
