using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.Scheduling.Domain.Packages;

public enum CustomerPackageSource
{
    /// <summary>Firma yetkilisi panelden elle tanımladı.</summary>
    Assigned = 0,
    /// <summary>Üye kendi satın aldı (iyzico tek seferlik ödeme).</summary>
    Purchased = 1,
}

/// <summary>
/// Bir üyeye tanımlanmış ders paketi bakiyesi. Randevu "paketten düş" ile alındığında
/// <see cref="Deduct"/>, iptalde <see cref="Refund"/> çağrılır; ad/ders sayısı, paket sonradan
/// değişse bile tarihçe bozulmasın diye atama anında denormalize edilir.
/// </summary>
/// <remarks>
/// <see cref="ExpiresAtUtc"/> yalnızca <see cref="Assign"/> ile, oluşturma anında hesaplanır ve
/// bilerek değiştirilemez (yerinde güncelleyen bir "yenile" mutator'ı KASITLI olarak yok). Bir
/// paket yenilenecekse yeni bir CustomerPackage satırı oluşturulmalı - aksi halde
/// <see cref="LastReminderDaysBeforeExpiry"/> dedup alanı eski (küçük) değerde takılı kalıp yeni,
/// daha kaba hatırlatma eşiklerinin bir daha asla tetiklenmemesine yol açar (bkz. ProcessPackageExpiriesCommand).
/// </remarks>
public sealed class CustomerPackage
{
    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid LessonPackageId { get; private set; }

    public string PackageName { get; private set; } = null!;

    public int TotalSessions { get; private set; }

    public int RemainingSessions { get; private set; }

    public DateTimeOffset AssignedAtUtc { get; private set; }

    public DateTimeOffset? ExpiresAtUtc { get; private set; }

    /// <summary>Süre dolmadan en son hangi eşik (gün) için hatırlatma gönderildi; null = hiç gönderilmedi.</summary>
    public int? LastReminderDaysBeforeExpiry { get; private set; }

    public CustomerPackageSource Source { get; private set; }

    /// <summary>Yalnızca <see cref="CustomerPackageSource.Purchased"/> için dolu; iyzico ödeme referans kodu.</summary>
    public string? PaymentReferenceCode { get; private set; }

    private CustomerPackage()
    {
    }

    public static CustomerPackage Assign(
        Guid customerId, LessonPackage package, CustomerPackageSource source, string? paymentReferenceCode = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new CustomerPackage
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            LessonPackageId = package.Id,
            PackageName = package.Name,
            TotalSessions = package.SessionCount,
            RemainingSessions = package.SessionCount,
            AssignedAtUtc = now,
            ExpiresAtUtc = package.ValidityDays is { } days ? now.AddDays(days) : null,
            Source = source,
            PaymentReferenceCode = paymentReferenceCode,
        };
    }

    public void Deduct()
    {
        if (RemainingSessions <= 0)
            throw new DomainException("package_exhausted", "Paketinizde kullanılabilir ders kalmadı.");

        RemainingSessions--;
    }

    public void Refund()
    {
        if (RemainingSessions < TotalSessions)
            RemainingSessions++;
    }

    public void MarkReminderSent(int daysBeforeExpiry) => LastReminderDaysBeforeExpiry = daysBeforeExpiry;
}
