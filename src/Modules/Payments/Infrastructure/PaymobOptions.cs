namespace Bartrix.Modules.Payments.Infrastructure;

public sealed class PaymobOptions
{
    public const string SectionName = "Paymob";

    public string ApiKey { get; init; } = string.Empty;
    public int IntegrationId { get; init; }
    public int IframeId { get; init; }
    public string HmacSecret { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = "https://accept.paymob.com/api";
}
