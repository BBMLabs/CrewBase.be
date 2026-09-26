using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Domain.Companies;

namespace RowingClub.Identity.Infrastructure.Persistence.Configurations;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable(IdentityTableNames.Companies);
        builder.HasAggregateRootDefaults();

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(c => c.Name).IsUnique();

        builder.Property(c => c.Subdomain).HasMaxLength(63).IsRequired();
        builder.HasIndex(c => c.Subdomain).IsUnique();

        builder.Property(c => c.DatabaseName).HasMaxLength(63).IsRequired();

        builder.Property(c => c.LogoPath).HasMaxLength(500);
        builder.Property(c => c.Tagline).HasMaxLength(200).IsRequired();
        builder.Property(c => c.AboutText).HasMaxLength(2000).IsRequired();
        builder.Property(c => c.InstagramUrl).HasMaxLength(500);
        builder.Property(c => c.FacebookUrl).HasMaxLength(500);
        builder.Property(c => c.YoutubeUrl).HasMaxLength(500);
        builder.Property(c => c.LinkedinUrl).HasMaxLength(500);
        builder.Property(c => c.XUrl).HasMaxLength(500);
        builder.Property(c => c.WhatsappUrl).HasMaxLength(500);
        builder.Property(c => c.TelegramUrl).HasMaxLength(500);
        builder.Property(c => c.PinterestUrl).HasMaxLength(500);
        builder.Property(c => c.GoogleMapsUrl).HasMaxLength(500);
        builder.Property(c => c.Phone).HasMaxLength(20);
        builder.Property(c => c.ContactEmail).HasMaxLength(254);
        builder.Property(c => c.Address).HasMaxLength(500);
        builder.Property(c => c.SeoTitle).HasMaxLength(Company.SeoTitleMaxLength);
        builder.Property(c => c.SeoDescription).HasMaxLength(Company.SeoDescriptionMaxLength);
        builder.Property(c => c.SeoKeywords).HasMaxLength(Company.SeoKeywordsMaxLength);
        builder.Property(c => c.GoogleSiteVerification).HasMaxLength(Company.GoogleSiteVerificationMaxLength);
        builder.Property(c => c.GoogleAnalyticsId).HasMaxLength(Company.GoogleAnalyticsIdMaxLength);
        builder.Property(c => c.TaxNumber).HasMaxLength(11);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(c => c.Plan).HasConversion<string>().HasMaxLength(50);
    }
}

public sealed class CompanyGalleryImageConfiguration : IEntityTypeConfiguration<CompanyGalleryImage>
{
    public void Configure(EntityTypeBuilder<CompanyGalleryImage> builder)
    {
        builder.ToTable(IdentityTableNames.CompanyGalleryImages);
        builder.HasKey(i => i.Id);

        builder.Property(i => i.ImagePath).HasMaxLength(500).IsRequired();

        builder.HasIndex(i => i.CompanyId);
        builder.HasOne<Company>().WithMany().HasForeignKey(i => i.CompanyId).OnDelete(DeleteBehavior.Cascade);
    }
}
