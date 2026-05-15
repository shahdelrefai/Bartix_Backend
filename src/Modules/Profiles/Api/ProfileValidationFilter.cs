using Bartrix.Modules.Profiles.Application;
using Microsoft.AspNetCore.Http;

namespace Bartrix.Modules.Profiles.Api;

public sealed class ProfileValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (ProfileValidationException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["profile"] = new[] { exception.Message }
            });
        }
    }
}
