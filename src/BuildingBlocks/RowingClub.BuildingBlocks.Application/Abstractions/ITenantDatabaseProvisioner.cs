namespace RowingClub.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Firma kaydı sırasında firmanın kendi veritabanını oluşturur ve şemasını kurar.
/// Uygulaması Scheduling.Infrastructure'dadır (tenant şemasını o modül bilir); Identity yalnızca
/// bu soyutlamaya bağımlıdır.
/// </summary>
public interface ITenantDatabaseProvisioner
{
    Task ProvisionAsync(string databaseName, CancellationToken cancellationToken);
}
