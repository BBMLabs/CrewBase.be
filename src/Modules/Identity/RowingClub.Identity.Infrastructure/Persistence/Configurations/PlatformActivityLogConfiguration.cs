using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RowingClub.Identity.Domain.Platform;

namespace RowingClub.Identity.Infrastructure.Persistence.Configurations;

public sealed class PlatformActivityLogConfiguration : IEntityTypeConfiguration<PlatformActivityLog>
{
    public void Configure(EntityTypeBuilder<PlatformActivityLog> builder)
    {
        builder.ToTable(IdentityTableNames.PlatformActivityLogs);
        builder.HasKey(l => l.Id);

        builder.Property(l => l.ActorEmail).HasMaxLength(254).IsRequired();
        builder.Property(l => l.Action).HasMaxLength(100).IsRequired();
        builder.Property(l => l.IpAddress).HasMaxLength(64);
        builder.Property(l => l.UserAgent).HasMaxLength(500);

        builder.HasIndex(l => l.AtUtc);
        builder.HasIndex(l => l.TargetCompanyId);
    }
}
