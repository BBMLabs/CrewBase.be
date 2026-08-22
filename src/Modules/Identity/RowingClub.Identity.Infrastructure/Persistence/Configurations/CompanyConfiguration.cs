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
        builder.Property(c => c.Phone).HasMaxLength(20);
        builder.Property(c => c.ContactEmail).HasMaxLength(254);
        builder.Property(c => c.Address).HasMaxLength(500);
        builder.Property(c => c.TaxNumber).HasMaxLength(11);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(50);
    }
}
