using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Domain.Users;

namespace RowingClub.Identity.Infrastructure.Persistence.Configurations;

public sealed class CredentialConfiguration : IEntityTypeConfiguration<Credential>
{
    public void Configure(EntityTypeBuilder<Credential> builder)
    {
        builder.ToTable(IdentityTableNames.Credentials);
        builder.HasAggregateRootDefaults();

        builder.HasIndex(c => c.UserId).IsUnique();
    }
}
