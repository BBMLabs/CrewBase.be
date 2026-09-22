using Microsoft.EntityFrameworkCore;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Domain.Companies;

namespace RowingClub.Identity.Infrastructure.Repositories;

public sealed class CompanyRepository(RowingClubDbContext context) : ICompanyRepository
{
    public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Set<Company>().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Company?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken) =>
        context.Set<Company>().FirstOrDefaultAsync(c => c.Subdomain == subdomain, cancellationToken);

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken) =>
        context.Set<Company>().AnyAsync(c => c.Name == name, cancellationToken);

    public Task<bool> ExistsBySubdomainAsync(string subdomain, CancellationToken cancellationToken) =>
        context.Set<Company>().AnyAsync(c => c.Subdomain == subdomain, cancellationToken);

    public Task<List<Company>> GetByStatusAsync(CompanyStatus status, CancellationToken cancellationToken) =>
        context.Set<Company>().Where(c => c.Status == status).ToListAsync(cancellationToken);

    public Task<List<Company>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        ids.Count == 0
            ? Task.FromResult(new List<Company>())
            : context.Set<Company>().Where(c => ids.Contains(c.Id)).ToListAsync(cancellationToken);

    public Task<List<Company>> GetAllAsync(CancellationToken cancellationToken) =>
        context.Set<Company>().ToListAsync(cancellationToken);

    public void Add(Company company) => context.Set<Company>().Add(company);

    public void Update(Company company) => context.Set<Company>().Update(company);

    public void Remove(Company company) => context.Set<Company>().Remove(company);
}
