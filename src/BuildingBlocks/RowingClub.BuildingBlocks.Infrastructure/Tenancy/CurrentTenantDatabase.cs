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
    private int _maxInstructors;
    private int _maxManagers;
    private int _maxEmployees;
    private bool _canExportData;
    private bool _hasAdvancedReports;
    private bool _hasAutomaticDuesReminders;

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

    public int MaxInstructors => IsSet
        ? _maxInstructors
        : throw new InvalidOperationException("Tenant veritabanı bu istek için çözülmedi.");

    public int MaxManagers => IsSet
        ? _maxManagers
        : throw new InvalidOperationException("Tenant veritabanı bu istek için çözülmedi.");

    public int MaxEmployees => IsSet
        ? _maxEmployees
        : throw new InvalidOperationException("Tenant veritabanı bu istek için çözülmedi.");

    public bool CanExportData => IsSet
        ? _canExportData
        : throw new InvalidOperationException("Tenant veritabanı bu istek için çözülmedi.");

    public bool HasAdvancedReports => IsSet
        ? _hasAdvancedReports
        : throw new InvalidOperationException("Tenant veritabanı bu istek için çözülmedi.");

    public bool HasAutomaticDuesReminders => IsSet
        ? _hasAutomaticDuesReminders
        : throw new InvalidOperationException("Tenant veritabanı bu istek için çözülmedi.");

    public void Set(
        Guid companyId, string databaseName, string subdomain, string plan,
        int maxBranches, int maxMembers, int maxBoats, int maxInstructors,
        int maxManagers, int maxEmployees, bool canExportData,
        bool hasAdvancedReports, bool hasAutomaticDuesReminders)
    {
        _companyId = companyId;
        _databaseName = databaseName;
        _subdomain = subdomain;
        _plan = plan;
        _maxBranches = maxBranches;
        _maxMembers = maxMembers;
        _maxBoats = maxBoats;
        _maxInstructors = maxInstructors;
        _maxManagers = maxManagers;
        _maxEmployees = maxEmployees;
        _canExportData = canExportData;
        _hasAdvancedReports = hasAdvancedReports;
        _hasAutomaticDuesReminders = hasAutomaticDuesReminders;
    }
}
