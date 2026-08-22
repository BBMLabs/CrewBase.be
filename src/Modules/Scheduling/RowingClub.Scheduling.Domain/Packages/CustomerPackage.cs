using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.Scheduling.Domain.Packages;

/// <summary>
/// Bir üyeye tanımlanmış ders paketi bakiyesi. Randevu "paketten düş" ile alındığında
/// <see cref="Deduct"/>, iptalde <see cref="Refund"/> çağrılır; ad/ders sayısı, paket sonradan
/// değişse bile tarihçe bozulmasın diye atama anında denormalize edilir.
/// </summary>
public sealed class CustomerPackage
{
    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid LessonPackageId { get; private set; }

    public string PackageName { get; private set; } = null!;

    public int TotalSessions { get; private set; }

    public int RemainingSessions { get; private set; }

    public DateTimeOffset AssignedAtUtc { get; private set; }

    private CustomerPackage()
    {
    }

    public static CustomerPackage Assign(Guid customerId, LessonPackage package) => new()
    {
        Id = Guid.NewGuid(),
        CustomerId = customerId,
        LessonPackageId = package.Id,
        PackageName = package.Name,
        TotalSessions = package.SessionCount,
        RemainingSessions = package.SessionCount,
        AssignedAtUtc = DateTimeOffset.UtcNow,
    };

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
}
