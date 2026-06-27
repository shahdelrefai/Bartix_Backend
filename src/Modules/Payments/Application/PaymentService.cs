using Bartrix.BuildingBlocks.Notifications;
using Bartrix.BuildingBlocks.Wallet;
using Bartrix.Modules.Payments.Contracts;
using Bartrix.Modules.Payments.Domain;
using Bartrix.Modules.Payments.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Bartrix.Modules.Payments.Application;

public sealed class PaymentService(
    PaymentsDbContext dbContext,
    IWalletService walletService,
    INotificationPublisher notifications,
    IPaymobClient paymobClient,
    IOptions<PaymobOptions> paymobOptions,
    TimeProvider timeProvider,
    ILogger<PaymentService> logger) : IPaymentService
{
    private readonly PaymobOptions _paymob = paymobOptions.Value;

    public async Task<PaymentResponse> CreateAsync(Guid buyerId, CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            throw new PaymentValidationException("Amount must be greater than zero.");

        // Seller premium status is unknown at creation time (no cross-module EF)
        // Fee is applied at webhook credit time; stored as 0 for now.
        var payment = new Payment(buyerId, request.SellerId, request.ProductTitle, request.Amount, 0m, timeProvider.GetUtcNow());
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Attempt Paymob flow; fall back gracefully if not configured
        string? paymentKey = null;
        string? iframeUrl = null;

        if (!string.IsNullOrWhiteSpace(_paymob.ApiKey))
        {
            try
            {
                var billing = request.BillingData is { } bd
                    ? new PaymobBillingData(bd.FirstName, bd.LastName, bd.Email, bd.PhoneNumber)
                    : new PaymobBillingData("NA", "NA", "na@na.com", "00000000000");

                var authToken = await paymobClient.GetAuthTokenAsync(cancellationToken);
                var orderId = await paymobClient.RegisterOrderAsync(
                    authToken,
                    (long)(request.Amount * 100),
                    payment.Id.ToString(),
                    cancellationToken);

                paymentKey = await paymobClient.GetPaymentKeyAsync(
                    authToken, orderId,
                    (long)(request.Amount * 100),
                    billing,
                    _paymob.IntegrationId,
                    cancellationToken);

                iframeUrl = $"{_paymob.BaseUrl.Replace("/api", "")}/acceptance/iframes/{_paymob.IframeId}?payment_token={paymentKey}";
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Paymob integration failed for payment {Id}; returning without payment key.", payment.Id);
            }
        }

        return Map(payment, paymentKey, iframeUrl);
    }

    public async Task<PaymentResponse?> GetAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await dbContext.Payments
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == paymentId, cancellationToken);
        return payment is null ? null : Map(payment);
    }

    public async Task ProcessWebhookAsync(PaymobWebhookPayload payload, CancellationToken cancellationToken)
    {
        if (!payload.Success) return;

        Payment? payment = null;

        if (payload.OurPaymentId.HasValue)
            payment = await dbContext.Payments.SingleOrDefaultAsync(p => p.Id == payload.OurPaymentId.Value, cancellationToken);

        if (payment is null && payload.TransactionId is not null)
            payment = await dbContext.Payments.SingleOrDefaultAsync(p => p.PaymobTransactionId == payload.TransactionId, cancellationToken);

        if (payment is null || payment.IsCredited) return;

        // Determine seller premium status via raw SQL to avoid cross-module EF
        var isSellerPremium = await IsSellerPremiumAsync(payment.SellerId, dbContext.Database.GetDbConnection() as Npgsql.NpgsqlConnection, cancellationToken);
        var fee = FeeCalculator.Calculate(payment.Amount, isSellerPremium);
        var net = payment.Amount - fee;

        var now = timeProvider.GetUtcNow();
        payment.Complete(payload.TransactionId, now);
        payment.SetFeeAmount(fee);
        payment.MarkCredited(now);
        await dbContext.SaveChangesAsync(cancellationToken);

        await walletService.CreditAsync(
            payment.SellerId,
            net,
            payment.Id.ToString(),
            $"Payment received for {payment.ProductTitle} (fee: {fee:F2} EGP)",
            cancellationToken);

        await notifications.PublishAsync(new NotificationPublishRequest(
            payment.SellerId,
            TitleKey: "paymentReceivedTitle",
            BodyKey: "paymentReceivedBody",
            Type: "wallet_update",
            BodyArgs: new Dictionary<string, string>
            {
                ["amount"] = net.ToString("F2"),
                ["product"] = payment.ProductTitle,
            },
            RelatedId: payment.Id.ToString()), cancellationToken);
    }

    private static async Task<bool> IsSellerPremiumAsync(
        Guid sellerId,
        Npgsql.NpgsqlConnection? conn,
        CancellationToken ct)
    {
        if (conn is null) return false;
        try
        {
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT is_premium_active FROM auth.user_accounts WHERE id = $1", conn);
            cmd.Parameters.Add(new NpgsqlParameter { Value = sellerId });
            var result = await cmd.ExecuteScalarAsync(ct);
            return result is true;
        }
        catch { return false; }
    }

    private static PaymentResponse Map(Payment p, string? paymentKey = null, string? iframeUrl = null) =>
        new(p.Id, p.BuyerId, p.SellerId, p.ProductTitle,
            p.Amount, p.FeeAmount, p.Amount - p.FeeAmount,
            p.Status, p.PaymobTransactionId, p.IsCredited, p.CreatedAtUtc,
            paymentKey, iframeUrl);
}
