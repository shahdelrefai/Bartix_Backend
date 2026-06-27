using Bartrix.Modules.Reports.Application;
using Microsoft.AspNetCore.Http;

namespace Bartrix.Modules.Reports.Api;

public sealed class ReportsValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (ReportsValidationException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["report"] = new[] { ex.Message }
            });
        }
    }
}
