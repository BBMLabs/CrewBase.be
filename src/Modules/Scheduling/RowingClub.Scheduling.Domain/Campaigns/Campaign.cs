using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain.Customers;

namespace RowingClub.Scheduling.Domain.Campaigns;

public sealed class Campaign
{
    public Guid Id { get; private set; }

    public Guid LessonPackageId { get; private set; }

    public DateTimeOffset StartsAtUtc { get; private set; }

    public DateTimeOffset EndsAtUtc { get; private set; }

    public decimal Price { get; private set; }

    /// <summary>İkisi de null ise kampanya tüm üyelere görünür; doluysa yalnızca bu derece aralığındaki üyelere.</summary>
    public int? MinLevel { get; private set; }

    public int? MaxLevel { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private Campaign()
    {
    }

    public static Campaign Create(
        Guid lessonPackageId, DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, decimal price,
        int? minLevel, int? maxLevel)
    {
        Validate(startsAtUtc, endsAtUtc, price, minLevel, maxLevel);
        return new Campaign
        {
            Id = Guid.NewGuid(),
            LessonPackageId = lessonPackageId,
            StartsAtUtc = startsAtUtc,
            EndsAtUtc = endsAtUtc,
            Price = price,
            MinLevel = minLevel,
            MaxLevel = maxLevel,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Update(DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, decimal price, int? minLevel, int? maxLevel)
    {
        Validate(startsAtUtc, endsAtUtc, price, minLevel, maxLevel);
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        Price = price;
        MinLevel = minLevel;
        MaxLevel = maxLevel;
    }

    public bool IsActiveAt(DateTimeOffset atUtc) => atUtc >= StartsAtUtc && atUtc <= EndsAtUtc;

    public bool IsVisibleToLevel(int level) => (MinLevel is null || level >= MinLevel) && (MaxLevel is null || level <= MaxLevel);

    private static void Validate(
        DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, decimal price, int? minLevel, int? maxLevel)
    {
        if (endsAtUtc <= startsAtUtc)
            throw new DomainException("invalid_campaign_window", "Kampanya bitiş tarihi başlangıçtan sonra olmalıdır.");

        if (price < 0)
            throw new DomainException("invalid_campaign_price", "Kampanya fiyatı negatif olamaz.");

        if (minLevel is < 0 or > Customer.MaxLevel)
            throw new DomainException("invalid_campaign_level", $"Minimum derece 0-{Customer.MaxLevel} arasında olmalıdır.");

        if (maxLevel is < 0 or > Customer.MaxLevel)
            throw new DomainException("invalid_campaign_level", $"Maksimum derece 0-{Customer.MaxLevel} arasında olmalıdır.");

        if (minLevel is not null && maxLevel is not null && minLevel > maxLevel)
            throw new DomainException("invalid_campaign_level", "Minimum derece maksimum dereceden büyük olamaz.");
    }
}
