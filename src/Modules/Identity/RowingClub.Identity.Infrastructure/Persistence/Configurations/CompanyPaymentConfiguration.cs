using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Domain.Companies.Billing;

namespace RowingClub.Identity.Infrastructure.Persistence.Configurations;

public sealed class CompanyPaymentConfiguration : IEntityTypeConfiguration<CompanyPayment>
{
    public void Configure(EntityTypeBuilder<CompanyPayment> builder)
    {
        builder.ToTable(IdentityTableNames.CompanyPayments);
        builder.HasAggregateRootDefaults();

        builder.HasIndex(p => p.CompanyId);

        builder.Property(p => p.Plan).HasConversion<string>().HasMaxLength(50);
        builder.Property(p => p.Amount).HasColumnType("numeric(10,2)");
        builder.Property(p => p.Currency).HasMaxLength(3);
        builder.Property(p => p.Kind).HasConversion<string>().HasMaxLength(30);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(p => p.IyzicoPaymentReferenceCode).HasMaxLength(100);
        // Webhook tekrarına (replay) karşı dedup: aynı iyzico ödeme referansı ikinci kez asla eklenemez.
        builder.HasIndex(p => p.IyzicoPaymentReferenceCode).IsUnique();

        builder.Property(p => p.FailureReason).HasMaxLength(500);
    }
}
