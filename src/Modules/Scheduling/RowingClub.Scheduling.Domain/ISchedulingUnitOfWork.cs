namespace RowingClub.Scheduling.Domain;

/// <summary>
/// Tenant veritabanına yazan handler'lar için ayrı unit-of-work. Global TransactionBehavior yalnızca
/// katalog (ana) veritabanının IUnitOfWork'ünü kaydettiği için tenant tarafı bunu kullanır.
/// </summary>
public interface ISchedulingUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
