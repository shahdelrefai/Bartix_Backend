using Bartrix.Modules.Messaging.Application;
using Microsoft.AspNetCore.Http;

namespace Bartrix.Modules.Messaging.Api;

public sealed class MessagingValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (MessagingValidationException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["message"] = new[] { exception.Message }
            });
        }
    }
}
