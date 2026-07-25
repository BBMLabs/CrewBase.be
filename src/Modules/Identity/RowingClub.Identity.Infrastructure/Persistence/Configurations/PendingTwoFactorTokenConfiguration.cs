using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RowingClub.Identity.Domain.Tokens;

namespace RowingClub.Identity.Infrastructure.Persistence.Configurations;

public sealed class PendingTwoFactorTokenConfiguration : IEntityTypeConfiguration<PendingTwoFactorToken>
{
    public void Configure(EntityTypeBuilder<PendingTwoFactorToken> builder)
    {
        builder.ToTable(IdentityTableNames.PendingTwoFactorTokens);
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(t => t.DeviceInfo).HasMaxLength(500);
        builder.Property(t => t.UserId).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();
    }
}
