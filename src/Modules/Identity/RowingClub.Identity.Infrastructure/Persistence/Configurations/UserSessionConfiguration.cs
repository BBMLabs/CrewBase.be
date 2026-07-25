using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Domain.Tokens;

namespace RowingClub.Identity.Infrastructure.Persistence.Configurations;

public sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable(IdentityTableNames.UserSessions);
        builder.HasAggregateRootDefaults();

        builder.HasIndex(s => s.RefreshTokenFamilyId).IsUnique();
        builder.HasIndex(s => s.UserId);
    }
}
