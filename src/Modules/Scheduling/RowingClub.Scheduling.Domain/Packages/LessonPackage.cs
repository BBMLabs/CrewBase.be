using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.Scheduling.Domain.Packages;

/// <summary>Firmanın kendi tanımladığı ders paketi (ör. "8 Derslik Başlangıç Paketi").</summary>
public sealed class LessonPackage
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public int SessionCount { get; private set; }

    public decimal Price { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>Görsel dosyasının /uploads altındaki göreli yolu (bkz. IFileStorageService). Null = görsel yok.</summary>
    public string? ImagePath { get; private set; }

    /// <summary>Bu paketten oluşan bir CustomerPackage kaç gün geçerli olur; null = süresiz.</summary>
    public int? ValidityDays { get; private set; }

    /// <summary>İkisi de null ise her zaman satılabilir; doluysa yalnızca bu tarih aralığında (kampanya).</summary>
    public DateTimeOffset? CampaignStartsAtUtc { get; private set; }

    public DateTimeOffset? CampaignEndsAtUtc { get; private set; }

    /// <summary>Kampanya penceresi aktifken geçerli olan indirimli fiyat; null ise kampanya sırasında da normal Price uygulanır.</summary>
    public decimal? CampaignPrice { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private LessonPackage()
    {
    }

    public static LessonPackage Create(
        string name, string? description, int sessionCount, decimal price,
        int? validityDays, DateTimeOffset? campaignStartsAtUtc, DateTimeOffset? campaignEndsAtUtc,
        decimal? campaignPrice = null)
    {
        Validate(sessionCount, price, validityDays, campaignStartsAtUtc, campaignEndsAtUtc, campaignPrice);
        return new LessonPackage
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            SessionCount = sessionCount,
            Price = price,
            IsActive = true,
            ValidityDays = validityDays,
            CampaignStartsAtUtc = campaignStartsAtUtc,
            CampaignEndsAtUtc = campaignEndsAtUtc,
            CampaignPrice = campaignPrice,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Update(
        string name, string? description, int sessionCount, decimal price, bool isActive,
        int? validityDays, DateTimeOffset? campaignStartsAtUtc, DateTimeOffset? campaignEndsAtUtc,
        decimal? campaignPrice = null)
    {
        Validate(sessionCount, price, validityDays, campaignStartsAtUtc, campaignEndsAtUtc, campaignPrice);
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        SessionCount = sessionCount;
        Price = price;
        IsActive = isActive;
        ValidityDays = validityDays;
        CampaignStartsAtUtc = campaignStartsAtUtc;
        CampaignEndsAtUtc = campaignEndsAtUtc;
        CampaignPrice = campaignPrice;
    }

    public void SetImage(string path) => ImagePath = path;

    public void ClearImage() => ImagePath = null;

    /// <summary>Aktif VE (kampanya penceresi yok ya da şu an o pencerenin içindeyiz) ise true.
    /// Hem üye self-servis satın alma listesi hem admin'in "üyeye ata" seçimi bunu kullanır.</summary>
    public bool IsCurrentlyPurchasable(DateTimeOffset nowUtc)
    {
        if (!IsActive)
            return false;

        if (CampaignStartsAtUtc is { } starts && nowUtc < starts)
            return false;

        if (CampaignEndsAtUtc is { } ends && nowUtc > ends)
            return false;

        return true;
    }

    /// <summary>Şu an kampanya penceresi aktifse ve bir kampanya fiyatı tanımlıysa onu, aksi halde normal Price'ı döner.</summary>
    public decimal GetEffectivePrice(DateTimeOffset nowUtc)
    {
        if (CampaignPrice is { } campaignPrice &&
            CampaignStartsAtUtc is { } starts && nowUtc >= starts &&
            CampaignEndsAtUtc is { } ends && nowUtc <= ends)
            return campaignPrice;

        return Price;
    }

    private static void Validate(
        int sessionCount, decimal price, int? validityDays,
        DateTimeOffset? campaignStartsAtUtc, DateTimeOffset? campaignEndsAtUtc, decimal? campaignPrice)
    {
        if (sessionCount < 1)
            throw new DomainException("invalid_session_count", "Paket en az 1 ders içermelidir.");

        if (price < 0)
            throw new DomainException("invalid_price", "Paket fiyatı negatif olamaz.");

        if (validityDays is <= 0)
            throw new DomainException("invalid_validity_days", "Geçerlilik süresi en az 1 gün olmalıdır.");

        if (campaignStartsAtUtc is not null && campaignEndsAtUtc is not null && campaignStartsAtUtc >= campaignEndsAtUtc)
            throw new DomainException("invalid_campaign_window", "Kampanya bitiş tarihi başlangıçtan sonra olmalıdır.");

        if (campaignPrice is < 0)
            throw new DomainException("invalid_campaign_price", "Kampanya fiyatı negatif olamaz.");

        if (campaignPrice is not null && (campaignStartsAtUtc is null || campaignEndsAtUtc is null))
            throw new DomainException("invalid_campaign_price", "Kampanya fiyatı yalnızca kampanya tarih aralığı tanımlıysa girilebilir.");
    }
}
