namespace RowingClub.Scheduling.Domain.Branches;

public sealed class Branch
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Address { get; private set; }

    public string? Phone { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private Branch()
    {
    }

    public static Branch Create(string name, string? address, string? phone) => new()
    {
        Id = Guid.NewGuid(),
        Name = name.Trim(),
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim(),
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
        IsActive = true,
        CreatedAtUtc = DateTimeOffset.UtcNow,
    };

    public void Update(string name, string? address, string? phone, bool isActive)
    {
        Name = name.Trim();
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        IsActive = isActive;
    }
}
