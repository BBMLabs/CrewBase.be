namespace RowingClub.Scheduling.Domain.Packages;

public interface ICustomerPackageRepository
{
    Task<List<CustomerPackage>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken);

    Task<CustomerPackage?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<List<CustomerPackage>> GetAllAsync(CancellationToken cancellationToken);

    void Add(CustomerPackage customerPackage);

    void Remove(CustomerPackage customerPackage);
}
