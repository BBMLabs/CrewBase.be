using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.Identity.Domain.Companies;

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Company?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);

    Task<bool> ExistsBySubdomainAsync(string subdomain, CancellationToken cancellationToken);

    Task<List<Company>> GetByStatusAsync(CompanyStatus status, CancellationToken cancellationToken);

    Task<List<Company>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    void Add(Company company);

    void Update(Company company);
}
