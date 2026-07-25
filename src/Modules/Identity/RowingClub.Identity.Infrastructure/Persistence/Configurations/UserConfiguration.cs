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
    }
}
