using Microsoft.EntityFrameworkCore;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Domain.Companies;

namespace RowingClub.Identity.Infrastructure.Repositories;

public sealed class CompanyGalleryImageRepository(RowingClubDbContext context) : ICompanyGalleryImageRepository
{
    public Task<List<CompanyGalleryImage>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken) =>
        context.Set<CompanyGalleryImage>()
            .Where(i => i.CompanyId == companyId)
            .OrderBy(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<CompanyGalleryImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Set<CompanyGalleryImage>().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<int> CountByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken) =>
        context.Set<CompanyGalleryImage>().CountAsync(i => i.CompanyId == companyId, cancellationToken);

    public void Add(CompanyGalleryImage image) => context.Set<CompanyGalleryImage>().Add(image);

    public void Remove(CompanyGalleryImage image) => context.Set<CompanyGalleryImage>().Remove(image);
}
