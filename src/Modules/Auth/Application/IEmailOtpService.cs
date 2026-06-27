namespace Bartrix.Modules.Auth.Application;

public interface IEmailOtpService
{
    Task SendOtpAsync(string email, string purpose, CancellationToken cancellationToken);
    Task<bool> VerifyOtpAsync(string email, string purpose, string code, CancellationToken cancellationToken);
}
