using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.Identity.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable(IdentityTableNames.Users);
        builder.HasAggregateRootDefaults();

        builder.Property(u => u.Email)
            .HasConversion(v => v.Value, v => EmailAddress.Create(v))
            .IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.Status).HasConversion<string>();
        builder.Property(u => u.Role).HasConversion<string>();

        builder.Property(u => u.CompanyId).IsRequired(false);
        builder.HasIndex(u => u.CompanyId);

        builder.Property(u => u.TwoFactorMethod).HasMaxLength(20).HasDefaultValue("None");
        builder.Property(u => u.TwoFactorSecret).HasMaxLength(256).IsRequired(false);
    }
}
