namespace RowingClub.Identity.Domain.Companies.Billing;

public interface ICompanySubscriptionRepository
{
    Task<CompanySubscription?> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken);

    Task<CompanySubscription?> GetByIyzicoSubscriptionReferenceCodeAsync(
        string referenceCode, CancellationToken cancellationToken);

    /// <summary>Dönem sonu geçmiş ama bekleyen paketi hâlâ uygulanmamış tüm abonelikler (güvenlik-ağı worker'ı için).</summary>
    Task<List<CompanySubscription>> GetDuePendingPlanChangesAsync(
        DateTimeOffset asOfUtc, CancellationToken cancellationToken);

    void Add(CompanySubscription subscription);

    void Update(CompanySubscription subscription);

    void Remove(CompanySubscription subscription);
}
