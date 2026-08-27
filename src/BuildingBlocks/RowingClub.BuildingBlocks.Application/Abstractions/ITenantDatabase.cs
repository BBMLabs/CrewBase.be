namespace RowingClub.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Her firmanın kendi PostgreSQL veritabanı vardır (database-per-tenant). API katmanı, isteği
/// hangi firmanın (subdomain veya oturumdaki CompanyId üzerinden) yaptığını çözdükten sonra bu
/// scoped bağlamı doldurur; tenant DbContext'i bağlantı dizesini buradan kurar.
/// </summary>
public interface ITenantDatabase
{
    bool IsSet { get; }

    Guid CompanyId { get; }

    string DatabaseName { get; }

    string Subdomain { get; }

    /// <summary>Firmanın geçerli abonelik paketi ve üst sınırları — kayıt/oluşturma akışları bunlara karşı denetim yapar.</summary>
    string Plan { get; }

    int MaxBranches { get; }

    int MaxMembers { get; }

    int MaxBoats { get; }

    void Set(Guid companyId, string databaseName, string subdomain, string plan, int maxBranches, int maxMembers, int maxBoats);
}
