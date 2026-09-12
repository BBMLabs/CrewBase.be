namespace RowingClub.Identity.Domain.Companies;

public interface ICompanyGalleryImageRepository
{
    Task<List<CompanyGalleryImage>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken);

    Task<CompanyGalleryImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<int> CountByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken);

    void Add(CompanyGalleryImage image);

    void Remove(CompanyGalleryImage image);
}
