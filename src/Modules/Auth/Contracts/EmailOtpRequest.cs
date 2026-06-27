namespace Bartrix.Modules.Auth.Contracts;

public sealed record EmailOtpRequest(string Email);

public sealed record VerifyEmailOtpRequest(string Email, string Code);

public sealed record PasswordResetConfirmRequest(string Email, string Code, string NewPassword);

public sealed record LogoutRequest(string RefreshToken);

public sealed record PremiumActivateRequest(string PaymobPaymentId);

public sealed record PremiumStatusResponse(bool IsActive, DateTimeOffset? ExpiresAt);

public sealed record PremiumPlanResponse(string Id, string Name, decimal Price, int DurationDays);
