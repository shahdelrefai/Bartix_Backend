using System.Security.Claims;
using Bartrix.Modules.Notifications.Application;
using Bartrix.Modules.Notifications.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Bartrix.Modules.Notifications.Api;

public static class NotificationsEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications");
        group.WithTags("Notifications");
        group.RequireAuthorization();
        group.AddEndpointFilter<NotificationsValidationFilter>();

        group.MapGet("/", async (
            ClaimsPrincipal principal,
            INotificationService notificationService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var response = await notificationService.GetForUserAsync(userId, cancellationToken);
            return Results.Ok(response);
        });

        group.MapGet("/unread-count", async (
            ClaimsPrincipal principal,
            INotificationService notificationService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            var count = await notificationService.GetUnreadCountAsync(userId, cancellationToken);
            return Results.Ok(new UnreadCountResponse(count));
        });

        group.MapPost("/{notificationId:guid}/read", async (
            ClaimsPrincipal principal,
            Guid notificationId,
            INotificationService notificationService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            await notificationService.MarkReadAsync(userId, notificationId, cancellationToken);
            return Results.NoContent();
        });

        group.MapPost("/read-all", async (
            ClaimsPrincipal principal,
            INotificationService notificationService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            await notificationService.MarkAllReadAsync(userId, cancellationToken);
            return Results.NoContent();
        });

        return app;
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");

        if (!Guid.TryParse(value, out var userId))
        {
            throw new NotificationValidationException("Authenticated user identifier is invalid.");
        }

        return userId;
    }
}
