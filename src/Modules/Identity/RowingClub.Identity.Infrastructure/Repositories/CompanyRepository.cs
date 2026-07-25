using Microsoft.EntityFrameworkCore;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Domain.Companies;

namespace RowingClub.Identity.Infrastructure.Repositories;

public sealed class CompanyRepository(RowingClubDbContext context) : ICompanyRepository
{
    public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Set<Company>().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken) =>
        context.Set<Company>().AnyAsync(c => c.Name == name, cancellationToken);

    public Task<List<Company>> GetByStatusAsync(CompanyStatus status, CancellationToken cancellationToken) =>
        context.Set<Company>().Where(c => c.Status == status).ToListAsync(cancellationToken);

    public void Add(Company company) => context.Set<Company>().Add(company);

    public void Update(Company company) => context.Set<Company>().Update(company);
}
