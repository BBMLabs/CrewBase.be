namespace RowingClub.Identity.Domain.Companies;

/// <summary>
/// Sabit paketlerin aylık gösterim fiyatı (fiyatlandırma sayfasıyla birebir). Bu yalnızca
/// checkout-öncesi arayüz gösterimi içindir - gerçekte tahsil edilen tutar her zaman iyzico'nun
/// API cevabından okunur (bkz. CompanyPayment.Amount). Buradaki tutarlar iyzico panelindeki
/// Pricing Plan tutarlarıyla elle senkron tutulmalıdır; biri değişirse diğeri de güncellenmelidir.
/// </summary>
public static class CompanyPlanPricingCatalog
{
    private static readonly Dictionary<CompanyPlan, decimal> MonthlyTry = new()
    {
        [CompanyPlan.Mico] = 0m,
        [CompanyPlan.Tayfa] = 490m,
        [CompanyPlan.Kaptan] = 890m,
        [CompanyPlan.Amiral] = 1490m,
    };

    public static decimal MonthlyPriceFor(CompanyPlan plan) =>
        MonthlyTry.TryGetValue(plan, out var price) ? price : 0m;
}
