using Bartrix.Modules.Notifications.Application;
using Microsoft.AspNetCore.Http;

namespace Bartrix.Modules.Notifications.Api;

public sealed class NotificationsValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (NotificationValidationException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["notification"] = new[] { exception.Message }
            });
        }
    }
}
