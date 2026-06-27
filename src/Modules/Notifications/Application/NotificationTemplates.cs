namespace Bartrix.Modules.Notifications.Application;

/// <summary>
/// Localized notification templates ported from the Flutter
/// <c>localizedNotificationStrings</c> map (en/ar). Placeholders like {sender},
/// {message}, {rating}, {product}, {reporter} are substituted at render time.
/// </summary>
public static class NotificationTemplates
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Strings =
        new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["en"] = new Dictionary<string, string>
            {
                ["newOfferTitle"] = "New Trade Offer",
                ["newOfferBody"] = "{sender} sent you a trade offer.",
                ["counterOfferTitle"] = "New Counter Offer",
                ["counterOfferBody"] = "{sender} sent a counter offer.",
                ["tradeAcceptedTitle"] = "Trade Accepted!",
                ["tradeAcceptedBody"] = "Your trade offer has been accepted!",
                ["tradeRejectedTitle"] = "Trade Rejected",
                ["tradeRejectedBody"] = "Your trade offer was rejected.",
                ["tradeAutoRejectedBody"] = "Your offer was cancelled because the item is no longer available.",
                ["tradeCompletedTitle"] = "Trade Completed!",
                ["tradeCompletedBody"] = "The trade has been confirmed as completed.",
                ["newMessageTitle"] = "New Message",
                ["newMessageBody"] = "{sender}: {message}",
                ["newReviewTitle"] = "New Review received",
                ["newReviewBody"] = "{sender} left you a review: {rating}⭐",
                ["newReportTitle"] = "New Report Submitted",
                ["newReportBody"] = "A new report was submitted for \"{product}\" by {reporter}.",
                ["paymentReceivedTitle"] = "Payment Received!",
                ["paymentReceivedBody"] = "You received {amount} EGP for \"{product}\".",
                ["withdrawalCompletedTitle"] = "Withdrawal Completed",
                ["withdrawalCompletedBody"] = "Your withdrawal of {amount} EGP has been processed.",
                ["withdrawalRejectedTitle"] = "Withdrawal Rejected",
                ["withdrawalRejectedBody"] = "Your withdrawal request of {amount} EGP was rejected.",
            },
            ["ar"] = new Dictionary<string, string>
            {
                ["newOfferTitle"] = "عرض مبادلة جديد",
                ["newOfferBody"] = "أرسل لك {sender} عرض مبادلة.",
                ["counterOfferTitle"] = "عرض مقابل جديد",
                ["counterOfferBody"] = "أرسل {sender} عرضاً مقابلاً.",
                ["tradeAcceptedTitle"] = "تم قبول المبادلة!",
                ["tradeAcceptedBody"] = "تم قبول عرض المبادلة الخاص بك!",
                ["tradeRejectedTitle"] = "تم رفض المبادلة",
                ["tradeRejectedBody"] = "تم رفض عرض المبادلة الخاص بك.",
                ["tradeAutoRejectedBody"] = "تم إلغاء عرضك لأن السلعة لم تعد متوفرة.",
                ["tradeCompletedTitle"] = "اكتملت المبادلة!",
                ["tradeCompletedBody"] = "تم تأكيد اكتمال المبادلة.",
                ["newMessageTitle"] = "رسالة جديدة",
                ["newMessageBody"] = "{sender}: {message}",
                ["newReviewTitle"] = "تم استلام تقييم جديد",
                ["newReviewBody"] = "ترك لك {sender} تقييمًا: {rating}⭐",
                ["newReportTitle"] = "تم تقديم بلاغ جديد",
                ["newReportBody"] = "تم تقديم بلاغ جديد عن \"{product}\" بواسطة {reporter}.",
                ["paymentReceivedTitle"] = "تم استلام الدفعة!",
                ["paymentReceivedBody"] = "استلمت {amount} جنيه مقابل \"{product}\".",
                ["withdrawalCompletedTitle"] = "تم سحب الأموال",
                ["withdrawalCompletedBody"] = "تمت معالجة طلب سحب مبلغ {amount} جنيه.",
                ["withdrawalRejectedTitle"] = "تم رفض طلب السحب",
                ["withdrawalRejectedBody"] = "تم رفض طلب سحب مبلغ {amount} جنيه.",
            },
        };

    public static string Render(string languageCode, string key, IReadOnlyDictionary<string, string>? args)
    {
        var lang = !string.IsNullOrWhiteSpace(languageCode) && Strings.ContainsKey(languageCode)
            ? languageCode
            : "en";

        if (!Strings[lang].TryGetValue(key, out var template))
        {
            // Fall back to English, then to the raw key.
            if (!Strings["en"].TryGetValue(key, out template))
            {
                return key;
            }
        }

        if (args is not null)
        {
            foreach (var (name, value) in args)
            {
                template = template.Replace("{" + name + "}", value);
            }
        }

        return template;
    }
}
