using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.Identity.Domain.Companies;

public enum CompanyStatus
{
    PendingApproval = 0,
    Active = 1,
    Suspended = 2,
}

public sealed class Company : AggregateRoot<Guid>
{
    public string Name { get; private set; } = null!;

    /// <summary>Firmanın site adresi: {Subdomain}.faturebase.com (şimdilik mock domain).</summary>
    public string Subdomain { get; private set; } = null!;

    /// <summary>Firmanın kendi PostgreSQL veritabanının adı (database-per-tenant).</summary>
    public string DatabaseName { get; private set; } = null!;

    public string? LogoPath { get; private set; }

    public string? Phone { get; private set; }

    public string? ContactEmail { get; private set; }

    public string? Address { get; private set; }

    /// <summary>Vergi kimlik no (10 hane) veya T.C. kimlik no (11 hane, şahıs işletmesi için).</summary>
    public string? TaxNumber { get; private set; }

    public CompanyStatus Status { get; private set; }

    public CompanyPlan Plan { get; private set; }

    /// <summary>Yalnızca <see cref="Plan"/> == <see cref="CompanyPlan.Custom"/> iken anlamlıdır; Master panelden görüşülerek girilir.</summary>
    public int? CustomMaxBranches { get; private set; }

    public int? CustomMaxMembers { get; private set; }

    public int? CustomMaxBoats { get; private set; }

    public int? CustomMaxInstructors { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? ApprovedAtUtc { get; private set; }

    public Guid? ApprovedByUserId { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    private Company()
    {
    }

    private Company(
        Guid id, string name, string subdomain, string databaseName,
        string? phone, string? contactEmail, string? address, string? taxNumber)
        : base(id)
    {
        Name = name;
        Subdomain = subdomain;
        DatabaseName = databaseName;
        Phone = phone;
        ContactEmail = contactEmail;
        Address = address;
        TaxNumber = taxNumber;

        // Firmalar artık onay beklemeden doğrudan aktif olarak açılır.
        Status = CompanyStatus.Active;

        // Her firma en düşük (ücretsiz) paketle başlar; yetkilisi dilerse sonradan yükseltir.
        Plan = CompanyPlan.Mico;

        CreatedAtUtc = DateTimeOffset.UtcNow;
        ApprovedAtUtc = CreatedAtUtc;
    }

    public static Company Register(
        string name, string subdomain, string databaseName,
        string? phone, string? contactEmail, string? address, string? taxNumber)
    {
        return new Company(Guid.NewGuid(), name, subdomain, databaseName, phone, contactEmail, address, taxNumber);
    }

    /// <summary>Firmayı aktifleştirir; askıya alınmış firmayı geri açmak için de kullanılır.</summary>
    public void Approve(Guid approvedByUserId)
    {
        if (Status == CompanyStatus.Active)
            return;

        Status = CompanyStatus.Active;
        ApprovedAtUtc = DateTimeOffset.UtcNow;
        ApprovedByUserId = approvedByUserId;
    }

    public void Suspend()
    {
        if (Status == CompanyStatus.Suspended)
            return;

        Status = CompanyStatus.Suspended;
    }

    public void SetLogo(string logoPath)
    {
        LogoPath = logoPath;
    }

    public void UpdateDetails(string name, string? phone, string? contactEmail, string? address, string? taxNumber)
    {
        Name = name;
        Phone = phone;
        ContactEmail = contactEmail;
        Address = address;
        TaxNumber = taxNumber;
    }

    /// <summary>Şu an geçerli üst sınırlar (sabit paketler için katalogdan, Custom için firmaya özel alanlardan).</summary>
    public CompanyPlanLimits PlanLimits => Plan == CompanyPlan.Custom
        ? new CompanyPlanLimits(CustomMaxBranches ?? 0, CustomMaxMembers ?? 0, CustomMaxBoats ?? 0, CustomMaxInstructors ?? 0)
        : CompanyPlanLimitsCatalog.For(Plan);

    /// <summary>Şu an geçerli nitel özellikler (dışa aktarma, gelişmiş raporlar, yönetici/çalışan kotaları...).</summary>
    public CompanyPlanFeatures PlanFeatures => CompanyPlanFeaturesCatalog.For(Plan);

    /// <summary>
    /// Firma yetkilisinin panelden kendi yaptığı paket değişikliği: yalnızca sabit paketler
    /// arasında, hem yükseltme hem düşürme yönünde. Düşürmede hedef paketin limitleri mevcut
    /// kullanımı (şube/aktif üye/tekne/eğitmen sayısı - tenant veritabanından çağıran tarafından
    /// çözülür) karşılamıyorsa reddedilir; önce fazlalığın silinmesi gerekir.
    /// </summary>
    public void ChangePlan(CompanyPlan newPlan, int usedBranches, int usedMembers, int usedBoats, int usedInstructors)
    {
        if (!CompanyPlanLimitsCatalog.IsFixed(newPlan))
            throw new DomainException("plan_not_selfserve", "Bu paket yalnızca satış ekibiyle görüşülerek tanımlanabilir.");

        CompanyPlanLimitsCatalog.EnsureUsageFits(newPlan, usedBranches, usedMembers, usedBoats, usedInstructors);

        Plan = newPlan;
        CustomMaxBranches = null;
        CustomMaxMembers = null;
        CustomMaxBoats = null;
        CustomMaxInstructors = null;
    }

    /// <summary>Master panelden serbest paket ataması; Custom seçildiğinde özel limitler zorunludur.</summary>
    public void SetPlan(
        CompanyPlan plan, int? customMaxBranches, int? customMaxMembers, int? customMaxBoats, int? customMaxInstructors)
    {
        if (plan == CompanyPlan.Custom)
        {
            if (customMaxBranches is null or <= 0 || customMaxMembers is null or <= 0 ||
                customMaxBoats is null or <= 0 || customMaxInstructors is null or <= 0)
                throw new DomainException("custom_limits_required", "Custom paket için şube/üye/tekne/eğitmen limitlerinin hepsi girilmelidir.");

            Plan = CompanyPlan.Custom;
            CustomMaxBranches = customMaxBranches;
            CustomMaxMembers = customMaxMembers;
            CustomMaxBoats = customMaxBoats;
            CustomMaxInstructors = customMaxInstructors;
            return;
        }

        Plan = plan;
        CustomMaxBranches = null;
        CustomMaxMembers = null;
        CustomMaxBoats = null;
        CustomMaxInstructors = null;
    }

    public void Delete()
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Restore()
    {
        if (!IsDeleted)
            return;

        IsDeleted = false;
        DeletedAtUtc = null;
    }
}
