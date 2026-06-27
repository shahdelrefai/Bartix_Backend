using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Bartrix.Modules.Payments.Infrastructure;

public sealed class PaymobHttpClient : IPaymobClient
{
    private readonly HttpClient _http;
    private readonly PaymobOptions _options;

    public PaymobHttpClient(HttpClient http, IOptions<PaymobOptions> options)
    {
        _http = http;
        _options = options.Value;
        _http.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
    }

    public async Task<string> GetAuthTokenAsync(CancellationToken cancellationToken)
    {
        var body = new { api_key = _options.ApiKey };
        var response = await _http.PostAsJsonAsync("auth/tokens", body, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        return json.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("Paymob auth token was null.");
    }

    public async Task<string> RegisterOrderAsync(
        string authToken,
        long amountCents,
        string merchantOrderId,
        CancellationToken cancellationToken)
    {
        var body = new
        {
            auth_token = authToken,
            delivery_needed = false,
            amount_cents = amountCents,
            currency = "EGP",
            merchant_order_id = merchantOrderId,
            items = Array.Empty<object>()
        };

        var response = await _http.PostAsJsonAsync("ecommerce/orders", body, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        return json.GetProperty("id").GetInt64().ToString();
    }

    public async Task<string> GetPaymentKeyAsync(
        string authToken,
        string orderId,
        long amountCents,
        PaymobBillingData billing,
        int integrationId,
        CancellationToken cancellationToken)
    {
        var body = new
        {
            auth_token = authToken,
            amount_cents = amountCents,
            expiration = 3600,
            order_id = orderId,
            billing_data = new
            {
                apartment = billing.Apartment,
                email = billing.Email,
                floor = billing.Floor,
                first_name = billing.FirstName,
                street = billing.Street,
                building = billing.Building,
                phone_number = billing.PhoneNumber,
                shipping_method = billing.ShippingMethod,
                postal_code = billing.PostalCode,
                city = billing.City,
                country = billing.Country,
                last_name = billing.LastName,
                state = billing.State
            },
            currency = "EGP",
            integration_id = integrationId
        };

        var response = await _http.PostAsJsonAsync("acceptance/payment_keys", body, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        return json.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("Paymob payment key was null.");
    }
}
