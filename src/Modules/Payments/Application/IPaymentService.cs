using Bartrix.Modules.Payments.Contracts;

namespace Bartrix.Modules.Payments.Application;

public interface IPaymentService
{
    Task<PaymentResponse> CreateAsync(Guid buyerId, CreatePaymentRequest request, CancellationToken cancellationToken);
    Task<PaymentResponse?> GetAsync(Guid paymentId, CancellationToken cancellationToken);
    Task ProcessWebhookAsync(PaymobWebhookPayload payload, CancellationToken cancellationToken);
}

public sealed record PaymobWebhookPayload(
    string? TransactionId,
    bool Success,
    Guid? OurPaymentId,
    decimal Amount);
