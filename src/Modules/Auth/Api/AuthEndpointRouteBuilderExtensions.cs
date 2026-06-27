using System.Security.Claims;
using Bartrix.Modules.Auth.Application;
using Bartrix.Modules.Auth.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;

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

        group.MapPost("/google", async (
            GoogleSignInRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            var response = await authService.GoogleSignInAsync(request, cancellationToken);
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

        group.MapPut("/me", [Authorize] async (
            ClaimsPrincipal principal,
            UpdateProfileRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }

            var response = await authService.UpdateProfileAsync(userId, request, cancellationToken);
            return Results.Ok(response);
        });

        group.MapPut("/me/language", [Authorize] async (
            ClaimsPrincipal principal,
            UpdateLanguageRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }

            await authService.UpdateLanguageAsync(userId, request, cancellationToken);
            return Results.NoContent();
        });

        group.MapPost("/users/{targetUserId:guid}/block", [Authorize] async (
            ClaimsPrincipal principal,
            Guid targetUserId,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }

            await authService.BlockUserAsync(userId, targetUserId, cancellationToken);
            return Results.NoContent();
        });

        group.MapPost("/users/{targetUserId:guid}/unblock", [Authorize] async (
            ClaimsPrincipal principal,
            Guid targetUserId,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }

            await authService.UnblockUserAsync(userId, targetUserId, cancellationToken);
            return Results.NoContent();
        });

        group.MapGet("/users/{targetUserId:guid}", [Authorize] async (
            Guid targetUserId,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            var response = await authService.GetPublicUserAsync(targetUserId, cancellationToken);
            return response is null ? Results.NotFound() : Results.Ok(response);
        });

        // ─── Email OTP ───────────────────────────────────────────────────────────

        group.MapPost("/otp/email/request", async (
            EmailOtpRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            await authService.RequestEmailOtpAsync(request.Email, "email-mfa", cancellationToken);
            return Results.Ok(new { message = "OTP sent. Check logs/email." });
        });

        group.MapPost("/otp/email/verify", async (
            VerifyEmailOtpRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            var response = await authService.VerifyEmailOtpAsync(request.Email, request.Code, cancellationToken);
            return Results.Ok(response);
        });

        // ─── Password reset ──────────────────────────────────────────────────────

        group.MapPost("/password-reset/request", async (
            EmailOtpRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            await authService.RequestPasswordResetAsync(request.Email, cancellationToken);
            return Results.Ok(new { message = "If the email is registered, a reset code was sent." });
        });

        group.MapPost("/password-reset/confirm", async (
            PasswordResetConfirmRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            await authService.ConfirmPasswordResetAsync(request.Email, request.Code, request.NewPassword, cancellationToken);
            return Results.Ok(new { message = "Password updated successfully." });
        });

        // ─── Logout ──────────────────────────────────────────────────────────────

        group.MapPost("/logout", [Authorize] async (
            LogoutRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            await authService.LogoutAsync(request.RefreshToken, cancellationToken);
            return Results.NoContent();
        });

        // ─── Premium subscription ────────────────────────────────────────────────

        group.MapGet("/premium/plans", () =>
        {
            var plans = new[]
            {
                new PremiumPlanResponse("monthly", "Premium Monthly", 99.00m, 30)
            };
            return Results.Ok(plans);
        });

        group.MapPost("/premium/activate", [Authorize] async (
            ClaimsPrincipal principal,
            PremiumActivateRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
                return Results.Unauthorized();

            await authService.ActivatePremiumAsync(userId, request.PaymobPaymentId, cancellationToken);
            return Results.Ok(new { message = "Premium activated for 30 days." });
        });

        group.MapGet("/premium/status", [Authorize] async (
            ClaimsPrincipal principal,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
                return Results.Unauthorized();

            var (isActive, expiresAt) = await authService.GetPremiumStatusAsync(userId, cancellationToken);
            return Results.Ok(new PremiumStatusResponse(isActive, expiresAt));
        });

        group.AddEndpointFilter<AuthValidationFilter>();

        return app;
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return Guid.TryParse(value, out userId);
    }
}
