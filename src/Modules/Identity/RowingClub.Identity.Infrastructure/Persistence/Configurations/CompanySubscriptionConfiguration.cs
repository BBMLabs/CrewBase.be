using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Domain.Companies.Billing;

namespace RowingClub.Identity.Infrastructure.Persistence.Configurations;

public sealed class CompanySubscriptionConfiguration : IEntityTypeConfiguration<CompanySubscription>
{
    public void Configure(EntityTypeBuilder<CompanySubscription> builder)
    {
        builder.ToTable(IdentityTableNames.CompanySubscriptions);
        builder.HasAggregateRootDefaults();

        builder.HasIndex(s => s.CompanyId).IsUnique();

        builder.Property(s => s.IyzicoCustomerReferenceCode).HasMaxLength(100);
        builder.Property(s => s.IyzicoSubscriptionReferenceCode).HasMaxLength(100);
        builder.HasIndex(s => s.IyzicoSubscriptionReferenceCode);

        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.PendingPlan).HasConversion<string>().HasMaxLength(50);
    }
}
