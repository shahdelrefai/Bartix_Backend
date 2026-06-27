using Bartrix.Modules.Admin.Application;
using Microsoft.AspNetCore.Http;

namespace Bartrix.Modules.Admin.Api;

public sealed class AdminValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (AdminValidationException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["admin"] = new[] { ex.Message }
            });
        }
    }
}
