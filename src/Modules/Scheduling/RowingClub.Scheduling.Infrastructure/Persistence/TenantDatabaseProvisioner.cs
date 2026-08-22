using Microsoft.EntityFrameworkCore;
using Npgsql;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Security.Encryption;
using RowingClub.Scheduling.Domain.Settings;

namespace RowingClub.Scheduling.Infrastructure.Persistence;

/// <summary>
/// Firma kaydında ve açılıştaki tenant migration taramasında çağrılır: sunucuda tenant veritabanı
/// yoksa oluşturur (CREATE DATABASE Postgres'te transaction dışında, katalog bağlantısı üzerinden
/// çalışır), şemayı EF migration'larıyla günceller ve varsayılan firma ayarlarını tohumlar.
/// Tekrar çağrılması güvenlidir (idempotent).
/// </summary>
public sealed class TenantDatabaseProvisioner(
    NpgsqlDataSource catalogDataSource,
    TenantConnectionStringFactory connectionStringFactory,
    IFieldEncryptor fieldEncryptor,
    IBlindIndexer blindIndexer)
    : ITenantDatabaseProvisioner
{
    public async Task ProvisionAsync(string databaseName, CancellationToken cancellationToken)
    {
        TenantConnectionStringFactory.EnsureSafeDatabaseName(databaseName);

        await using (var connection = await catalogDataSource.OpenConnectionAsync(cancellationToken))
        {
            await using var existsCommand = new NpgsqlCommand(
                "SELECT 1 FROM pg_database WHERE datname = @name", connection);
            existsCommand.Parameters.AddWithValue("name", databaseName);

            var exists = await existsCommand.ExecuteScalarAsync(cancellationToken) is not null;
            if (!exists)
            {
                // Ad EnsureSafeDatabaseName ile doğrulandı; CREATE DATABASE parametre alamaz.
                await using var createCommand = new NpgsqlCommand(
                    $"CREATE DATABASE \"{databaseName}\"", connection);
                await createCommand.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(connectionStringFactory.Create(databaseName))
            .Options;

        await using var tenantContext = new TenantDbContext(options, fieldEncryptor, blindIndexer);
        await tenantContext.Database.MigrateAsync(cancellationToken);

        // Her tenant tek bir ayar satırıyla başlar; firma bunu panelden özelleştirir.
        if (!await tenantContext.Settings.AnyAsync(cancellationToken))
        {
            tenantContext.Settings.Add(CompanySettings.Default());
            await tenantContext.SaveChangesAsync(cancellationToken);
        }
    }
}
