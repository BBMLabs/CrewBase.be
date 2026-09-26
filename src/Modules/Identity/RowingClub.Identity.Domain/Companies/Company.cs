using System.Text.RegularExpressions;
using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.Identity.Domain.Companies;

public enum CompanyStatus
{
    PendingApproval = 0,
    Active = 1,
    Suspended = 2,
}

public sealed partial class Company : AggregateRoot<Guid>
{
    public const int SeoTitleMaxLength = 70;
    public const int SeoDescriptionMaxLength = 170;
    public const int SeoKeywordsMaxLength = 255;
    public const int GoogleSiteVerificationMaxLength = 100;
    public const int GoogleAnalyticsIdMaxLength = 32;

    public string Name { get; private set; } = null!;

    /// <summary>Firmanın site adresi: {Subdomain}.faturebase.com (şimdilik mock domain).</summary>
    public string Subdomain { get; private set; } = null!;

    /// <summary>Firmanın kendi PostgreSQL veritabanının adı (database-per-tenant).</summary>
    public string DatabaseName { get; private set; } = null!;

    public string? LogoPath { get; private set; }

    public string Tagline { get; private set; } = null!;

    public string AboutText { get; private set; } = null!;

    public string? InstagramUrl { get; private set; }

    public string? FacebookUrl { get; private set; }

    public string? YoutubeUrl { get; private set; }

    public string? LinkedinUrl { get; private set; }

    public string? XUrl { get; private set; }

    public string? WhatsappUrl { get; private set; }

    public string? TelegramUrl { get; private set; }

    public string? PinterestUrl { get; private set; }

    public string? GoogleMapsUrl { get; private set; }

    public string? Phone { get; private set; }

    public string? ContactEmail { get; private set; }

    public string? Address { get; private set; }

    public string? SeoTitle { get; private set; }

    public string? SeoDescription { get; private set; }

    public string? SeoKeywords { get; private set; }

    public string? GoogleSiteVerification { get; private set; }

    public string? GoogleAnalyticsId { get; private set; }

    public bool AllowIndexing { get; private set; } = true;

    /// <summary>Vergi kimlik no (10 hane) veya T.C. kimlik no (11 hane, şahıs işletmesi için).</summary>
    public string? TaxNumber { get; private set; }

    public CompanyStatus Status { get; private set; }

    public CompanyPlan Plan { get; private set; }

    /// <summary>Yalnızca <see cref="Plan"/> == <see cref="CompanyPlan.Custom"/> iken anlamlıdır; Master panelden görüşülerek girilir.</summary>
    public int? CustomMaxBranches { get; private set; }

    public int? CustomMaxMembers { get; private set; }

    public int? CustomMaxBoats { get; private set; }

    public int? CustomMaxInstructors { get; private set; }

    public int? CustomMaxManagers { get; private set; }

    public int? CustomMaxEmployees { get; private set; }

    public bool? CustomCanExportData { get; private set; }

    public bool? CustomHasAdvancedReports { get; private set; }

    public bool? CustomHasAutomaticDuesReminders { get; private set; }

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
        Tagline = DefaultSiteContent.Tagline;
        AboutText = DefaultSiteContent.AboutText;

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

    public void UpdateSiteContent(string tagline, string aboutText)
    {
        if (string.IsNullOrWhiteSpace(tagline))
            throw new DomainException("invalid_tagline", "Tanıtım cümlesi boş olamaz.");
        if (string.IsNullOrWhiteSpace(aboutText))
            throw new DomainException("invalid_about_text", "Hakkımızda metni boş olamaz.");

        Tagline = tagline.Trim();
        AboutText = aboutText.Trim();
    }

    public void UpdateSocialLinks(
        string? instagramUrl, string? facebookUrl, string? youtubeUrl, string? linkedinUrl,
        string? xUrl, string? whatsappUrl, string? telegramUrl, string? pinterestUrl)
    {
        InstagramUrl = NormalizeUrl(instagramUrl, "invalid_instagram_url", "Instagram bağlantısı");
        FacebookUrl = NormalizeUrl(facebookUrl, "invalid_facebook_url", "Facebook bağlantısı");
        YoutubeUrl = NormalizeUrl(youtubeUrl, "invalid_youtube_url", "YouTube bağlantısı");
        LinkedinUrl = NormalizeUrl(linkedinUrl, "invalid_linkedin_url", "LinkedIn bağlantısı");
        XUrl = NormalizeUrl(xUrl, "invalid_x_url", "X bağlantısı");
        WhatsappUrl = NormalizeUrl(whatsappUrl, "invalid_whatsapp_url", "WhatsApp bağlantısı");
        TelegramUrl = NormalizeUrl(telegramUrl, "invalid_telegram_url", "Telegram bağlantısı");
        PinterestUrl = NormalizeUrl(pinterestUrl, "invalid_pinterest_url", "Pinterest bağlantısı");
    }

    public void UpdateGoogleMapsUrl(string? googleMapsUrl)
    {
        GoogleMapsUrl = NormalizeUrl(googleMapsUrl, "invalid_google_maps_url", "Google Haritalar bağlantısı");
    }

    public void UpdateSeoSettings(
        string? seoTitle, string? seoDescription, string? seoKeywords,
        string? googleSiteVerification, string? googleAnalyticsId, bool allowIndexing)
    {
        var title = NormalizeText(seoTitle, SeoTitleMaxLength, "invalid_seo_title", "SEO başlığı");
        var description = NormalizeText(
            seoDescription, SeoDescriptionMaxLength, "invalid_seo_description", "SEO açıklaması");
        var keywords = NormalizeText(seoKeywords, SeoKeywordsMaxLength, "invalid_seo_keywords", "SEO anahtar kelimeleri");
        var verification = NormalizeText(
            googleSiteVerification, GoogleSiteVerificationMaxLength,
            "invalid_google_site_verification", "Google site doğrulama kodu");
        var analyticsId = NormalizeText(
            googleAnalyticsId, GoogleAnalyticsIdMaxLength, "invalid_google_analytics_id", "Google Analytics kimliği");

        if (analyticsId is not null && !GoogleAnalyticsIdPattern().IsMatch(analyticsId))
            throw new DomainException(
                "invalid_google_analytics_id",
                "Google Analytics kimliği G-XXXXXXXXXX biçiminde olmalıdır.");

        SeoTitle = title;
        SeoDescription = description;
        SeoKeywords = keywords;
        GoogleSiteVerification = verification;
        GoogleAnalyticsId = analyticsId;
        AllowIndexing = allowIndexing;
    }

    private static string? NormalizeText(string? value, int maxLength, string errorCode, string fieldLabel)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException(errorCode, $"{fieldLabel} en fazla {maxLength} karakter olabilir.");

        return trimmed;
    }

    [GeneratedRegex("^G-[A-Z0-9]{4,20}$", RegexOptions.CultureInvariant)]
    private static partial Regex GoogleAnalyticsIdPattern();

    private static string? NormalizeUrl(string? url, string errorCode, string fieldLabel)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var trimmed = url.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new DomainException(errorCode, $"{fieldLabel} geçerli bir http(s) bağlantısı olmalıdır.");

        return trimmed;
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
    public CompanyPlanFeatures PlanFeatures => Plan == CompanyPlan.Custom
        ? new CompanyPlanFeatures(
            CustomMaxManagers ?? 0, CustomMaxEmployees ?? 0, CustomCanExportData ?? false,
            CustomHasAdvancedReports ?? false, CustomHasAutomaticDuesReminders ?? false)
        : CompanyPlanFeaturesCatalog.For(Plan);

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

    /// <summary>Master panelden serbest paket ataması; Custom seçildiğinde özel limitler ve nitel özellikler zorunludur.</summary>
    public void SetPlan(
        CompanyPlan plan, int? customMaxBranches, int? customMaxMembers, int? customMaxBoats, int? customMaxInstructors,
        int? customMaxManagers = null, int? customMaxEmployees = null, bool? customCanExportData = null,
        bool? customHasAdvancedReports = null, bool? customHasAutomaticDuesReminders = null)
    {
        if (plan == CompanyPlan.Custom)
        {
            if (customMaxBranches is null or <= 0 || customMaxMembers is null or <= 0 ||
                customMaxBoats is null or <= 0 || customMaxInstructors is null or <= 0 ||
                customMaxManagers is null or <= 0 || customMaxEmployees is null or <= 0 ||
                customCanExportData is null || customHasAdvancedReports is null || customHasAutomaticDuesReminders is null)
                throw new DomainException(
                    "custom_limits_required",
                    "Custom paket için şube/üye/tekne/eğitmen/yönetici/çalışan limitleri ve nitel özelliklerin hepsi girilmelidir.");

            Plan = CompanyPlan.Custom;
            CustomMaxBranches = customMaxBranches;
            CustomMaxMembers = customMaxMembers;
            CustomMaxBoats = customMaxBoats;
            CustomMaxInstructors = customMaxInstructors;
            CustomMaxManagers = customMaxManagers;
            CustomMaxEmployees = customMaxEmployees;
            CustomCanExportData = customCanExportData;
            CustomHasAdvancedReports = customHasAdvancedReports;
            CustomHasAutomaticDuesReminders = customHasAutomaticDuesReminders;
            return;
        }

        Plan = plan;
        CustomMaxBranches = null;
        CustomMaxMembers = null;
        CustomMaxBoats = null;
        CustomMaxInstructors = null;
        CustomMaxManagers = null;
        CustomMaxEmployees = null;
        CustomCanExportData = null;
        CustomHasAdvancedReports = null;
        CustomHasAutomaticDuesReminders = null;
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
