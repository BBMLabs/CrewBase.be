namespace RowingClub.Scheduling.Domain.Branches;

public sealed class Branch
{
    public Guid Id { get; private set; }

    /// <summary>6 rakam + 2 büyük harften oluşan tekil şube kodu; şubenin kendi genel sitesinin adresinde kullanılır.</summary>
    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? Address { get; private set; }

    public string? Phone { get; private set; }

    public bool IsActive { get; private set; }

    public string? ManagerName { get; private set; }

    public string? ManagerPhone { get; private set; }

    public string? ManagerEmail { get; private set; }

    /// <summary>Vergi kimlik no (10 hane) veya T.C. kimlik no (11 hane) - firmayla aynı numara da olabilir.</summary>
    public string? TaxNumber { get; private set; }

    public string? Description { get; private set; }

    public string? LogoPath { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private Branch()
    {
    }

    public static Branch Create(
        string code, string name, string? address, string? phone,
        string? managerName = null, string? managerPhone = null, string? managerEmail = null,
        string? taxNumber = null, string? description = null) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        Name = name.Trim(),
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim(),
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
        IsActive = true,
        ManagerName = string.IsNullOrWhiteSpace(managerName) ? null : managerName.Trim(),
        ManagerPhone = string.IsNullOrWhiteSpace(managerPhone) ? null : managerPhone.Trim(),
        ManagerEmail = string.IsNullOrWhiteSpace(managerEmail) ? null : managerEmail.Trim().ToLowerInvariant(),
        TaxNumber = string.IsNullOrWhiteSpace(taxNumber) ? null : taxNumber.Trim(),
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
        CreatedAtUtc = DateTimeOffset.UtcNow,
    };

    public void Update(
        string name, string? address, string? phone, bool isActive,
        string? managerName, string? managerPhone, string? managerEmail,
        string? taxNumber, string? description)
    {
        Name = name.Trim();
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        IsActive = isActive;
        ManagerName = string.IsNullOrWhiteSpace(managerName) ? null : managerName.Trim();
        ManagerPhone = string.IsNullOrWhiteSpace(managerPhone) ? null : managerPhone.Trim();
        ManagerEmail = string.IsNullOrWhiteSpace(managerEmail) ? null : managerEmail.Trim().ToLowerInvariant();
        TaxNumber = string.IsNullOrWhiteSpace(taxNumber) ? null : taxNumber.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void SetLogo(string path) => LogoPath = path;

    public void ClearLogo() => LogoPath = null;
}
