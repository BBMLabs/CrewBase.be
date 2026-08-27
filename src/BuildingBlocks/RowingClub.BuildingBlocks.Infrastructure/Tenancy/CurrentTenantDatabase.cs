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
    private string? _plan;
    private int _maxBranches;
    private int _maxMembers;
    private int _maxBoats;

    public bool IsSet => _databaseName is not null;

    public Guid CompanyId => IsSet
        ? _companyId
        : throw new InvalidOperationException("Tenant veritabanı bu istek için çözülmedi.");

    public string DatabaseName => _databaseName
        ?? throw new InvalidOperationException("Tenant veritabanı bu istek için çözülmedi.");

    public string Subdomain => _subdomain
        ?? throw new InvalidOperationException("Tenant veritabanı bu istek için çözülmedi.");

    public string Plan => _plan
        ?? throw new InvalidOperationException("Tenant veritabanı bu istek için çözülmedi.");

    public int MaxBranches => IsSet
        ? _maxBranches
        : throw new InvalidOperationException("Tenant veritabanı bu istek için çözülmedi.");

    public int MaxMembers => IsSet
        ? _maxMembers
        : throw new InvalidOperationException("Tenant veritabanı bu istek için çözülmedi.");

    public int MaxBoats => IsSet
        ? _maxBoats
        : throw new InvalidOperationException("Tenant veritabanı bu istek için çözülmedi.");

    public void Set(
        Guid companyId, string databaseName, string subdomain, string plan,
        int maxBranches, int maxMembers, int maxBoats)
    {
        _companyId = companyId;
        _databaseName = databaseName;
        _subdomain = subdomain;
        _plan = plan;
        _maxBranches = maxBranches;
        _maxMembers = maxMembers;
        _maxBoats = maxBoats;
    }
}
