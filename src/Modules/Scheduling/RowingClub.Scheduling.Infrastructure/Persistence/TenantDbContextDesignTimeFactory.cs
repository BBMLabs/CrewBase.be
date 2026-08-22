using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using RowingClub.BuildingBlocks.Security.Encryption;

namespace RowingClub.Scheduling.Infrastructure.Persistence;

/// <summary>
/// Yalnızca dotnet-ef araçları için: TenantDbContext'in bağlantısı normalde istek anında
/// ITenantDatabase'den kurulduğundan, design-time'da DI üzerinden oluşturulamaz. Bu factory
/// araçlara sahte bağlantı ve sahte şifreleyicilerle context üretir; migration üretimi model
/// şemasına bakar, şifreleyiciler hiç çağrılmaz.
/// </summary>
public sealed class TenantDbContextDesignTimeFactory : IDesignTimeDbContextFactory<TenantDbContext>
{
    public TenantDbContext CreateDbContext(string[] args) =>
        new(
            new DbContextOptionsBuilder<TenantDbContext>()
                .UseNpgsql("Host=localhost;Database=tenant_design_time")
                .Options,
            new DesignTimeFieldEncryptor(),
            new DesignTimeBlindIndexer());

    private sealed class DesignTimeFieldEncryptor : IFieldEncryptor
    {
        public EncryptedValue Encrypt(string plaintext) =>
            throw new NotSupportedException("Design-time şifreleyici veri işleyemez.");

        public string Decrypt(EncryptedValue value) =>
            throw new NotSupportedException("Design-time şifreleyici veri işleyemez.");
    }

    private sealed class DesignTimeBlindIndexer : IBlindIndexer
    {
        public string ComputeBlindIndex(string normalizedValue) =>
            throw new NotSupportedException("Design-time indexer veri işleyemez.");
    }
}
