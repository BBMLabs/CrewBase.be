using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.Identity.Domain.Companies;

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);

    Task<List<Company>> GetByStatusAsync(CompanyStatus status, CancellationToken cancellationToken);

    void Add(Company company);

    void Update(Company company);
}
