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

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private LessonPackage()
    {
    }

    public static LessonPackage Create(string name, string? description, int sessionCount, decimal price)
    {
        Validate(sessionCount, price);
        return new LessonPackage
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            SessionCount = sessionCount,
            Price = price,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Update(string name, string? description, int sessionCount, decimal price, bool isActive)
    {
        Validate(sessionCount, price);
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        SessionCount = sessionCount;
        Price = price;
        IsActive = isActive;
    }

    private static void Validate(int sessionCount, decimal price)
    {
        if (sessionCount < 1)
            throw new DomainException("invalid_session_count", "Paket en az 1 ders içermelidir.");

        if (price < 0)
            throw new DomainException("invalid_price", "Paket fiyatı negatif olamaz.");
    }
}
