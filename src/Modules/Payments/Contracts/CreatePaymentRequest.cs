namespace Bartrix.Modules.Payments.Contracts;

public sealed record CreatePaymentRequest(
    Guid SellerId,
    string ProductTitle,
    decimal Amount,
    PaymentBillingData? BillingData = null);

public sealed record PaymentBillingData(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber);
