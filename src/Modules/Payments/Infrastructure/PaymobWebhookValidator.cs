using System.Security.Cryptography;
using System.Text;

namespace Bartrix.Modules.Payments.Infrastructure;

/// <summary>
/// Validates Paymob webhook HMAC-SHA512 signatures.
/// Paymob computes HMAC over a specific set of transaction object fields
/// in a fixed alphabetical order, using the account HMAC secret.
/// </summary>
public static class PaymobWebhookValidator
{
    // Paymob-documented ordered fields for HMAC computation
    private static readonly string[] HmacFields =
    [
        "amount_cents", "created_at", "currency", "error_occured",
        "has_parent_transaction", "id", "integration_id", "is_3d_secure",
        "is_auth", "is_capture", "is_refunded", "is_standalone_payment",
        "is_voided", "order", "owner", "pending", "source_data_pan",
        "source_data_sub_type", "source_data_type", "success"
    ];

    public static bool Validate(
        IReadOnlyDictionary<string, string> transactionFields,
        string providedHmac,
        string hmacSecret)
    {
        if (string.IsNullOrEmpty(providedHmac) || string.IsNullOrEmpty(hmacSecret))
            return false;

        var concatenated = string.Concat(
            HmacFields.Select(f => transactionFields.TryGetValue(f, out var v) ? v : string.Empty));

        var key = Encoding.UTF8.GetBytes(hmacSecret);
        var data = Encoding.UTF8.GetBytes(concatenated);
        var hash = HMACSHA512.HashData(key, data);
        var computed = Convert.ToHexString(hash).ToLowerInvariant();

        return string.Equals(computed, providedHmac.ToLowerInvariant(), StringComparison.Ordinal);
    }
}
