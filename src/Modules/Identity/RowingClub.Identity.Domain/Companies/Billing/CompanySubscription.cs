using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.Identity.Domain.Companies.Billing;

public enum CompanySubscriptionStatus
{
    /// <summary>Hiç ücretli abonelik başlatılmadı (firma Mico'da, kart kayıtlı değil).</summary>
    None = 0,
    Active = 1,
    /// <summary>Son yenileme tahsilatı başarısız oldu; iyzico kendi retry penceresinde (160 gün).</summary>
    PastDue = 2,
    Cancelled = 3,
}

/// <summary>
/// Bir firmanın iyzico Abonelik (Subscription) durumu: kayıtlı kart referansı, mevcut dönemin
/// bitiş tarihi ("sonraki ödeme günü") ve varsa dönem sonunda uygulanacak bekleyen paket düşürme.
/// <see cref="Company.Plan"/> her zaman firmanın *şu an* geçerli yetki/limit kaynağıdır ve bu
/// aggregate tarafından değiştirilmez - burası yalnızca faturalama/dönem durumunu taşır (bkz.
/// plan dosyasındaki mimari karar #1: Company'ye alan eklemek yerine ayrı aggregate).
/// </summary>
public sealed class CompanySubscription : AggregateRoot<Guid>
{
    public Guid CompanyId { get; private set; }

    public string? IyzicoCustomerReferenceCode { get; private set; }

    public string? IyzicoSubscriptionReferenceCode { get; private set; }

    public CompanySubscriptionStatus Status { get; private set; }

    /// <summary>Mevcut faturalama döneminin bitiş anı = "sonraki ödeme günü".</summary>
    public DateTimeOffset? CurrentPeriodEndUtc { get; private set; }

    /// <summary>Firma yetkilisi alt pakete geçiş talep ettiyse hedef paket; dönem sonunda uygulanır.</summary>
    public CompanyPlan? PendingPlan { get; private set; }

    public DateTimeOffset? PendingPlanEffectiveAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private CompanySubscription()
    {
    }

    private CompanySubscription(Guid id, Guid companyId) : base(id)
    {
        CompanyId = companyId;
        Status = CompanySubscriptionStatus.None;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    /// <summary>Firma için ilk kez (henüz hiç abonelik satırı yokken) oluşturulur - hâlâ ücretsiz/None durumdadır.</summary>
    public static CompanySubscription CreateEmpty(Guid companyId) => new(Guid.NewGuid(), companyId);

    /// <summary>İlk ücretli abonelik (ya da iptal sonrası yeniden abonelik) iyzico'da başarıyla başladığında çağrılır.</summary>
    public void Activate(
        string iyzicoCustomerReferenceCode, string iyzicoSubscriptionReferenceCode, DateTimeOffset periodEndUtc)
    {
        IyzicoCustomerReferenceCode = iyzicoCustomerReferenceCode;
        IyzicoSubscriptionReferenceCode = iyzicoSubscriptionReferenceCode;
        Status = CompanySubscriptionStatus.Active;
        CurrentPeriodEndUtc = periodEndUtc;
        PendingPlan = null;
        PendingPlanEffectiveAtUtc = null;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Bir yenileme (otomatik ya da yükseltme sonrası) tahsilatı başarılı olduğunda dönemi ileri taşır.</summary>
    public void RecordRenewal(DateTimeOffset newPeriodEndUtc)
    {
        Status = CompanySubscriptionStatus.Active;
        CurrentPeriodEndUtc = newPeriodEndUtc;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkPastDue()
    {
        Status = CompanySubscriptionStatus.PastDue;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkCancelled()
    {
        Status = CompanySubscriptionStatus.Cancelled;
        IyzicoSubscriptionReferenceCode = null;
        CurrentPeriodEndUtc = null;
        PendingPlan = null;
        PendingPlanEffectiveAtUtc = null;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Alt pakete geçiş talebi: dönem sonuna kadar ertelenir, hemen uygulanmaz.</summary>
    public void RequestPendingPlan(CompanyPlan plan, DateTimeOffset effectiveAtUtc)
    {
        if (PendingPlan is not null)
        {
            throw new DomainException(
                "pending_downgrade_exists",
                "Zaten bekleyen bir paket değişikliği var; önce onu iptal etmelisiniz.");
        }

        PendingPlan = plan;
        PendingPlanEffectiveAtUtc = effectiveAtUtc;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void ClearPendingPlan()
    {
        PendingPlan = null;
        PendingPlanEffectiveAtUtc = null;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Güvenlik-ağı worker'ı ya da webhook, dönem sonuna ulaşıldığında bekleyen geçişi uygulamadan önce çağırır.</summary>
    public CompanyPlan ApplyPendingPlan()
    {
        if (PendingPlan is not { } plan)
            throw new DomainException("no_pending_plan", "Bekleyen bir paket değişikliği yok.");

        PendingPlan = null;
        PendingPlanEffectiveAtUtc = null;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        return plan;
    }
}
