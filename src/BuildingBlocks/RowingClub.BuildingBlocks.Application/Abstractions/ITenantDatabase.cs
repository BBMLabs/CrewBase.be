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

    void Set(Guid companyId, string databaseName, string subdomain);
}
