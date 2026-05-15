using Bartrix.Modules.Services.Application;
using Microsoft.AspNetCore.Http;

namespace Bartrix.Modules.Services.Api;

public sealed class ServicesValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (ServicesValidationException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["service"] = new[] { exception.Message }
            });
        }
    }
}
