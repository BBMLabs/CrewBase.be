namespace RowingClub.Identity.Domain.Companies;

public sealed class CompanyGalleryImage
{
    public const int MaxImagesPerCompany = 12;

    public Guid Id { get; private set; }

    public Guid CompanyId { get; private set; }

    public string ImagePath { get; private set; } = null!;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private CompanyGalleryImage()
    {
    }

    public static CompanyGalleryImage Create(Guid companyId, string imagePath) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        ImagePath = imagePath,
        CreatedAtUtc = DateTimeOffset.UtcNow,
    };
}
