using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.Identity.Domain.Companies;

public enum CompanyStatus
{
    PendingApproval = 0,
    Active = 1,
    Suspended = 2,
}

public sealed class Company : AggregateRoot<Guid>
{
    public string Name { get; private set; } = null!;

    /// <summary>Firmanın site adresi: {Subdomain}.faturebase.com (şimdilik mock domain).</summary>
    public string Subdomain { get; private set; } = null!;

    /// <summary>Firmanın kendi PostgreSQL veritabanının adı (database-per-tenant).</summary>
    public string DatabaseName { get; private set; } = null!;

    public string? LogoPath { get; private set; }

    public string? Phone { get; private set; }

    public string? ContactEmail { get; private set; }

    public string? Address { get; private set; }

    public CompanyStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? ApprovedAtUtc { get; private set; }

    public Guid? ApprovedByUserId { get; private set; }

    private Company()
    {
    }

    private Company(
        Guid id, string name, string subdomain, string databaseName,
        string? phone, string? contactEmail, string? address)
        : base(id)
    {
        Name = name;
        Subdomain = subdomain;
        DatabaseName = databaseName;
        Phone = phone;
        ContactEmail = contactEmail;
        Address = address;

        // Firmalar artık onay beklemeden doğrudan aktif olarak açılır.
        Status = CompanyStatus.Active;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        ApprovedAtUtc = CreatedAtUtc;
    }

    public static Company Register(
        string name, string subdomain, string databaseName,
        string? phone, string? contactEmail, string? address)
    {
        return new Company(Guid.NewGuid(), name, subdomain, databaseName, phone, contactEmail, address);
    }

    /// <summary>Firmayı aktifleştirir; askıya alınmış firmayı geri açmak için de kullanılır.</summary>
    public void Approve(Guid approvedByUserId)
    {
        if (Status == CompanyStatus.Active)
            return;

        Status = CompanyStatus.Active;
        ApprovedAtUtc = DateTimeOffset.UtcNow;
        ApprovedByUserId = approvedByUserId;
    }

    public void Suspend()
    {
        if (Status == CompanyStatus.Suspended)
            return;

        Status = CompanyStatus.Suspended;
    }

    public void SetLogo(string logoPath)
    {
        LogoPath = logoPath;
    }
}
