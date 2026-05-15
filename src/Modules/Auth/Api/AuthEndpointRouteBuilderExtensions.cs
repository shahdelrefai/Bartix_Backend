using System.Security.Claims;
using Bartrix.Modules.Auth.Application;
using Bartrix.Modules.Auth.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Bartrix.Modules.Auth.Api;

public static class AuthEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");
        group.WithTags("Auth");

        group.MapPost("/register", async (
            RegisterRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            var response = await authService.RegisterAsync(request, cancellationToken);
            return Results.Ok(response);
        });

        group.MapPost("/login", async (
            LoginRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            var response = await authService.LoginAsync(request, cancellationToken);
            return Results.Ok(response);
        });

        group.MapPost("/phone/request", async (
            RequestPhoneOtpRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            var response = await authService.RequestPhoneOtpAsync(request, cancellationToken);
            return Results.Ok(response);
        });

        group.MapPost("/phone/verify", async (
            VerifyPhoneOtpRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            var response = await authService.VerifyPhoneOtpAsync(request, cancellationToken);
            return Results.Ok(response);
        });

        group.MapPost("/refresh", async (
            RefreshTokenRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            var response = await authService.RefreshAsync(request, cancellationToken);
            return Results.Ok(response);
        });

        group.MapGet("/me", [Authorize] async (
            ClaimsPrincipal principal,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue("sub");

            if (!Guid.TryParse(userIdValue, out var userId))
            {
                return Results.Unauthorized();
            }

            var response = await authService.GetMeAsync(userId, cancellationToken);
            return response is null ? Results.NotFound() : Results.Ok(response);
        });

        group.AddEndpointFilter<AuthValidationFilter>();

        return app;
    }
}
