namespace RowingClub.Identity.Domain.Companies;

/// <summary>
/// Platformun abonelik paketleri (fiyatlandırma sayfasındaki Miço/Tayfa/Kaptan/Amiral ile
/// birebir). <see cref="Custom"/> hariç tüm paketlerin limitleri sabittir (<see cref="CompanyPlanLimitsCatalog"/>);
/// Custom, firmaya özel görüşülmüş limitleri Company üzerindeki CustomMax* alanlarından okur.
/// </summary>
public enum CompanyPlan
{
    Mico = 0,
    Tayfa = 1,
    Kaptan = 2,
    Amiral = 3,
    Custom = 4,
}

/// <summary>Bir paketin izin verdiği üst sınırlar: şube, aktif üye, tekne sayısı.</summary>
public sealed record CompanyPlanLimits(int MaxBranches, int MaxMembers, int MaxBoats);

public static class CompanyPlanLimitsCatalog
{
    /// <summary>
    /// Sabit paketlerin limitleri (fiyatlandırma sayfasıyla aynı: şube/üye). Tekne limiti
    /// fiyatlandırma sayfasında sayısal olarak verilmez; üye sayısıyla orantılı makul bir üst
    /// sınır olarak burada tanımlanmıştır.
    /// </summary>
    private static readonly Dictionary<CompanyPlan, CompanyPlanLimits> Fixed = new()
    {
        [CompanyPlan.Mico] = new CompanyPlanLimits(MaxBranches: 1, MaxMembers: 15, MaxBoats: 3),
        [CompanyPlan.Tayfa] = new CompanyPlanLimits(MaxBranches: 1, MaxMembers: 50, MaxBoats: 8),
        [CompanyPlan.Kaptan] = new CompanyPlanLimits(MaxBranches: 2, MaxMembers: 100, MaxBoats: 15),
        [CompanyPlan.Amiral] = new CompanyPlanLimits(MaxBranches: 4, MaxMembers: 200, MaxBoats: 30),
    };

    /// <summary>Sabit paketlerin fiyatlandırma sayfasındaki sırası; self-servis yükseltme yalnızca bu sırada ileri gidebilir.</summary>
    public static readonly CompanyPlan[] UpgradeOrder =
        [CompanyPlan.Mico, CompanyPlan.Tayfa, CompanyPlan.Kaptan, CompanyPlan.Amiral];

    public static bool IsFixed(CompanyPlan plan) => Fixed.ContainsKey(plan);

    public static CompanyPlanLimits For(CompanyPlan plan) =>
        Fixed.TryGetValue(plan, out var limits)
            ? limits
            : throw new InvalidOperationException($"{plan} sabit bir paket değil; limitleri Company.CustomMax* alanlarından okunmalı.");
}
