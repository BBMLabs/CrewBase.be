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

    private Company(Guid id, string name, string? phone, string? contactEmail, string? address)
        : base(id)
    {
        Name = name;
        Phone = phone;
        ContactEmail = contactEmail;
        Address = address;
        Status = CompanyStatus.PendingApproval;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static Company Register(string name, string? phone, string? contactEmail, string? address)
    {
        return new Company(Guid.NewGuid(), name, phone, contactEmail, address);
    }

    public void Approve(Guid approvedByUserId)
    {
        if (Status != CompanyStatus.PendingApproval)
            throw new DomainException("company_not_pending", "Şirket zaten onaylanmış veya askıya alınmış.");

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
