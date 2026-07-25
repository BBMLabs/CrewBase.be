using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RowingClub.Identity.Domain.Users;

namespace RowingClub.Identity.Infrastructure.Persistence.Configurations;

public sealed class UserRecoveryCodeConfiguration : IEntityTypeConfiguration<UserRecoveryCode>
{
    public void Configure(EntityTypeBuilder<UserRecoveryCode> builder)
    {
        builder.ToTable(IdentityTableNames.RecoveryCodes);
        builder.HasKey(r => r.Id);
        builder.Property(r => r.CodeHash).HasMaxLength(128).IsRequired();
        builder.Property(r => r.UserId).IsRequired();
        builder.HasIndex(r => new { r.UserId, r.IsUsed });
    }
}
