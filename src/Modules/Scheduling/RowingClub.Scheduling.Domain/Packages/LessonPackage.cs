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

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private LessonPackage()
    {
    }

    public static LessonPackage Create(string name, string? description, int sessionCount, decimal price, int? validityDays)
    {
        Validate(sessionCount, price, validityDays);
        return new LessonPackage
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            SessionCount = sessionCount,
            Price = price,
            IsActive = true,
            ValidityDays = validityDays,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Update(
        string name, string? description, int sessionCount, decimal price, bool isActive, int? validityDays)
    {
        Validate(sessionCount, price, validityDays);
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        SessionCount = sessionCount;
        Price = price;
        IsActive = isActive;
        ValidityDays = validityDays;
    }

    public void SetImage(string path) => ImagePath = path;

    public void ClearImage() => ImagePath = null;

    private static void Validate(int sessionCount, decimal price, int? validityDays)
    {
        if (sessionCount < 1)
            throw new DomainException("invalid_session_count", "Paket en az 1 ders içermelidir.");

        if (price < 0)
            throw new DomainException("invalid_price", "Paket fiyatı negatif olamaz.");

        if (validityDays is <= 0)
            throw new DomainException("invalid_validity_days", "Geçerlilik süresi en az 1 gün olmalıdır.");
    }
}
