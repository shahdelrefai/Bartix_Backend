using Bartrix.Modules.Auth.Application;
using Microsoft.AspNetCore.Http;

namespace Bartrix.Modules.Auth.Api;

public sealed class AuthValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (AuthValidationException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["auth"] = new[] { exception.Message }
            });
        }
    }
}
