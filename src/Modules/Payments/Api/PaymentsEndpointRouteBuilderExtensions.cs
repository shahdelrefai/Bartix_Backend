using System.Security.Claims;
using System.Text.Json.Serialization;
using Bartrix.Modules.Payments.Application;
using Bartrix.Modules.Payments.Contracts;
using Bartrix.Modules.Payments.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Bartrix.Modules.Payments.Api;

public static class PaymentsEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapPaymentsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payments");
        group.WithTags("Payments");
        group.AddEndpointFilter<PaymentsValidationFilter>();

        // Authenticated: buyer creates a pending payment record and gets a Paymob iframe URL
        group.MapPost("/", async (
            ClaimsPrincipal principal,
            CreatePaymentRequest request,
            IPaymentService paymentService,
            CancellationToken cancellationToken) =>
        {
            var buyerId = GetUserId(principal);
            var response = await paymentService.CreateAsync(buyerId, request, cancellationToken);
            return Results.Created($"/api/payments/{response.Id}", response);
        }).RequireAuthorization();

        // Authenticated: check status of a specific payment
        group.MapGet("/{paymentId:guid}", async (
            ClaimsPrincipal principal,
            Guid paymentId,
            IPaymentService paymentService,
            CancellationToken cancellationToken) =>
        {
            var payment = await paymentService.GetAsync(paymentId, cancellationToken);
            if (payment is null) return Results.NotFound();
            var userId = GetUserId(principal);
            if (payment.BuyerId != userId && payment.SellerId != userId)
                return Results.Forbid();
            return Results.Ok(payment);
        }).RequireAuthorization();

        // Public: Paymob posts here after a transaction
        // HMAC-SHA512 is validated via the ?hmac= query parameter
        group.MapPost("/webhook", async (
            HttpRequest httpRequest,
            IPaymentService paymentService,
            IOptions<PaymobOptions> paymobOptions,
            CancellationToken cancellationToken) =>
        {
            var body = await httpRequest.ReadFromJsonAsync<PaymobWebhookBody>(cancellationToken);
            if (body is null) return Results.BadRequest();

            var obj = body.Obj;
            if (obj is null) return Results.Ok();

            // Validate HMAC signature if secret is configured
            var hmacSecret = paymobOptions.Value.HmacSecret;
            if (!string.IsNullOrWhiteSpace(hmacSecret))
            {
                var providedHmac = httpRequest.Query["hmac"].ToString();
                if (string.IsNullOrEmpty(providedHmac))
                    return Results.StatusCode(403);

                var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["amount_cents"]              = obj.AmountCents.ToString(),
                    ["created_at"]                = obj.CreatedAt ?? string.Empty,
                    ["currency"]                  = obj.Currency ?? string.Empty,
                    ["error_occured"]              = (obj.ErrorOccurred ?? false).ToString().ToLowerInvariant(),
                    ["has_parent_transaction"]     = (obj.HasParentTransaction ?? false).ToString().ToLowerInvariant(),
                    ["id"]                         = obj.Id?.ToString() ?? string.Empty,
                    ["integration_id"]             = obj.IntegrationId?.ToString() ?? string.Empty,
                    ["is_3d_secure"]               = (obj.Is3dSecure ?? false).ToString().ToLowerInvariant(),
                    ["is_auth"]                    = (obj.IsAuth ?? false).ToString().ToLowerInvariant(),
                    ["is_capture"]                 = (obj.IsCapture ?? false).ToString().ToLowerInvariant(),
                    ["is_refunded"]                = (obj.IsRefunded ?? false).ToString().ToLowerInvariant(),
                    ["is_standalone_payment"]      = (obj.IsStandalonePayment ?? false).ToString().ToLowerInvariant(),
                    ["is_voided"]                  = (obj.IsVoided ?? false).ToString().ToLowerInvariant(),
                    ["order"]                      = obj.OrderId?.ToString() ?? string.Empty,
                    ["owner"]                      = obj.Owner?.ToString() ?? string.Empty,
                    ["pending"]                    = (obj.Pending ?? false).ToString().ToLowerInvariant(),
                    ["source_data_pan"]            = obj.SourceDataPan ?? string.Empty,
                    ["source_data_sub_type"]       = obj.SourceDataSubType ?? string.Empty,
                    ["source_data_type"]           = obj.SourceDataType ?? string.Empty,
                    ["success"]                    = obj.Success.ToString().ToLowerInvariant(),
                };

                if (!PaymobWebhookValidator.Validate(fields, providedHmac, hmacSecret))
                    return Results.StatusCode(403);
            }

            Guid.TryParse(obj.MerchantOrderId, out var ourPaymentId);

            var payload = new PaymobWebhookPayload(
                TransactionId: obj.Id?.ToString(),
                Success: obj.Success,
                OurPaymentId: ourPaymentId == Guid.Empty ? null : ourPaymentId,
                Amount: obj.AmountCents / 100m);

            await paymentService.ProcessWebhookAsync(payload, cancellationToken);
            return Results.Ok();
        });

        return app;
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (!Guid.TryParse(value, out var userId))
            throw new PaymentValidationException("Authenticated user identifier is invalid.");
        return userId;
    }
}

// Extended deserialization of the Paymob webhook body for HMAC fields
file sealed record PaymobWebhookBody(
    [property: JsonPropertyName("obj")] PaymobWebhookObj? Obj);

file sealed record PaymobWebhookObj(
    [property: JsonPropertyName("id")] long? Id,
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("amount_cents")] long AmountCents,
    [property: JsonPropertyName("merchant_order_id")] string? MerchantOrderId,
    [property: JsonPropertyName("created_at")] string? CreatedAt,
    [property: JsonPropertyName("currency")] string? Currency,
    [property: JsonPropertyName("error_occured")] bool? ErrorOccurred,
    [property: JsonPropertyName("has_parent_transaction")] bool? HasParentTransaction,
    [property: JsonPropertyName("integration_id")] long? IntegrationId,
    [property: JsonPropertyName("is_3d_secure")] bool? Is3dSecure,
    [property: JsonPropertyName("is_auth")] bool? IsAuth,
    [property: JsonPropertyName("is_capture")] bool? IsCapture,
    [property: JsonPropertyName("is_refunded")] bool? IsRefunded,
    [property: JsonPropertyName("is_standalone_payment")] bool? IsStandalonePayment,
    [property: JsonPropertyName("is_voided")] bool? IsVoided,
    [property: JsonPropertyName("order")] long? OrderId,
    [property: JsonPropertyName("owner")] long? Owner,
    [property: JsonPropertyName("pending")] bool? Pending,
    [property: JsonPropertyName("source_data")] PaymobSourceData? SourceData)
{
    public string? SourceDataPan => SourceData?.Pan;
    public string? SourceDataSubType => SourceData?.SubType;
    public string? SourceDataType => SourceData?.Type;
}

file sealed record PaymobSourceData(
    [property: JsonPropertyName("pan")] string? Pan,
    [property: JsonPropertyName("sub_type")] string? SubType,
    [property: JsonPropertyName("type")] string? Type);
