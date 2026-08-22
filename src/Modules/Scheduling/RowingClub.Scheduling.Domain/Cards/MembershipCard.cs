using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.Scheduling.Domain.Cards;

public enum MembershipCardType
{
    Multisport = 0,
    Meditopia = 1,
}

public enum MembershipCardStatus
{
    Active = 0,
    Passive = 1,
    Expired = 2,
}

/// <summary>
/// Üyenin KENDİ kaydettiği üyelik kartı (Multisport / Meditopia). Üye başına her türden en fazla
/// bir kart olur ve yalnızca üye günceller. Kart numarası ve fotoğraf şifreli saklanır.
/// Geçerlilik tarihi geçmişse durum okuma anında Expired kabul edilir.
/// </summary>
public sealed class MembershipCard
{
    public const int MaxPhotoBytes = 5 * 1024 * 1024;

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public MembershipCardType Type { get; private set; }

    /// <summary>Kart numarası (yalnızca Multisport'ta zorunlu; şifreli saklanır).</summary>
    public string? CardNumber { get; private set; }

    public string CompanyName { get; private set; } = null!;

    public DateOnly? ExpiryDate { get; private set; }

    public MembershipCardStatus Status { get; private set; }

    /// <summary>Kart fotoğrafı, base64 (şifreli saklanır); null = fotoğraf yok.</summary>
    public string? PhotoBase64 { get; private set; }

    public string? PhotoContentType { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private MembershipCard()
    {
    }

    public static MembershipCard Create(Guid customerId, MembershipCardType type) => new()
    {
        Id = Guid.NewGuid(),
        CustomerId = customerId,
        Type = type,
        CompanyName = string.Empty,
        Status = MembershipCardStatus.Active,
        UpdatedAtUtc = DateTimeOffset.UtcNow,
    };

    public void Update(
        string? cardNumber, string companyName, DateOnly? expiryDate,
        MembershipCardStatus status, string? photoBase64, string? photoContentType)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            throw new DomainException("company_name_required", "Şirket adı zorunludur.");

        if (Type == MembershipCardType.Multisport)
        {
            if (string.IsNullOrWhiteSpace(cardNumber) || !cardNumber.Trim().All(char.IsDigit))
                throw new DomainException("card_number_required", "Multisport kart numarası zorunludur ve yalnızca rakamlardan oluşmalıdır.");
            CardNumber = cardNumber.Trim();
        }
        else
        {
            CardNumber = null;
        }

        if (photoBase64 is not null)
        {
            var estimatedBytes = photoBase64.Length * 3 / 4;
            if (estimatedBytes > MaxPhotoBytes)
                throw new DomainException("photo_too_large", "Kart fotoğrafı en fazla 5MB olabilir.");

            var contentType = (photoContentType ?? "").ToLowerInvariant();
            if (contentType is not ("image/jpeg" or "image/png" or "image/gif"))
                throw new DomainException("photo_invalid_type", "Kart fotoğrafı JPG, PNG veya GIF olmalıdır.");

            PhotoBase64 = photoBase64;
            PhotoContentType = contentType;
        }

        CompanyName = companyName.Trim();
        ExpiryDate = expiryDate;
        Status = status;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void RemovePhoto()
    {
        PhotoBase64 = null;
        PhotoContentType = null;
    }

    /// <summary>Geçerlilik tarihi geçtiyse durum otomatik "Süresi Dolmuş" kabul edilir.</summary>
    public MembershipCardStatus EffectiveStatus(DateOnly today) =>
        ExpiryDate is { } expiry && expiry < today ? MembershipCardStatus.Expired : Status;
}

public interface IMembershipCardRepository
{
    Task<List<MembershipCard>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken);

    Task<MembershipCard?> GetAsync(Guid customerId, MembershipCardType type, CancellationToken cancellationToken);

    void Add(MembershipCard card);
}
