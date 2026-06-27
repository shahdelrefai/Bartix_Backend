namespace Bartrix.Modules.Payments.Application;

public static class FeeCalculator
{
    private const decimal StandardRate = 0.05m;
    private const decimal PremiumRate = 0.02m;

    public static decimal Calculate(decimal grossAmount, bool isSellerPremium)
        => grossAmount * (isSellerPremium ? PremiumRate : StandardRate);

    public static decimal NetAmount(decimal grossAmount, bool isSellerPremium)
        => grossAmount - Calculate(grossAmount, isSellerPremium);
}
