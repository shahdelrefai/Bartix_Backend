namespace Bartrix.Modules.Payments.Contracts;

public sealed record PaymentResponse(
    Guid Id,
    Guid BuyerId,
    Guid SellerId,
    string ProductTitle,
    decimal Amount,
    decimal FeeAmount,
    decimal NetAmount,
    string Status,
    string? PaymobTransactionId,
    bool IsCredited,
    DateTimeOffset CreatedAtUtc,
    string? PaymentKey = null,
    string? IframeUrl = null);
