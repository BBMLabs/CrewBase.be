using RowingClub.BuildingBlocks.Application.Abstractions;

namespace RowingClub.BuildingBlocks.Infrastructure.Tenancy;

/// <summary>
/// Scoped taşıyıcı: istek başına bir kez API katmanınca doldurulur, tenant DbContext factory'si
/// tarafından okunur.
/// </summary>
public sealed class CurrentTenantDatabase : ITenantDatabase
{
    private Guid _companyId;
    private string? _databaseName;
    private string? _subdomain;

    public bool IsSet => _databaseName is not null;

    public Guid CompanyId => IsSet
        ? _companyId
        : throw new InvalidOperationException("Tenant veritabanı bu istek için çözülmedi.");

    public string DatabaseName => _databaseName
        ?? throw new InvalidOperationException("Tenant veritabanı bu istek için çözülmedi.");

    public string Subdomain => _subdomain
        ?? throw new InvalidOperationException("Tenant veritabanı bu istek için çözülmedi.");

    public void Set(Guid companyId, string databaseName, string subdomain)
    {
        _companyId = companyId;
        _databaseName = databaseName;
        _subdomain = subdomain;
    }
}
