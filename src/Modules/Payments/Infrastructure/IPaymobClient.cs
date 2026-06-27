namespace Bartrix.Modules.Payments.Infrastructure;

public interface IPaymobClient
{
    Task<string> GetAuthTokenAsync(CancellationToken cancellationToken);
    Task<string> RegisterOrderAsync(string authToken, long amountCents, string merchantOrderId, CancellationToken cancellationToken);
    Task<string> GetPaymentKeyAsync(string authToken, string orderId, long amountCents, PaymobBillingData billing, int integrationId, CancellationToken cancellationToken);
}

public sealed record PaymobBillingData(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string Apartment = "NA",
    string Floor = "NA",
    string Street = "NA",
    string Building = "NA",
    string ShippingMethod = "PKG",
    string PostalCode = "NA",
    string City = "NA",
    string Country = "EG",
    string State = "NA");
