namespace RowingClub.Scheduling.Domain.Instructors;

/// <summary>Firmanın eğitmeni/hocası; seanslara otomatik atanır, panelden yönetilir.</summary>
public sealed class Instructor
{
    public Guid Id { get; private set; }

    public string FullName { get; private set; } = null!;

    public string? Phone { get; private set; }

    public string? Email { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private Instructor()
    {
    }

    public static Instructor Create(string fullName, string? phone, string? email) => new()
    {
        Id = Guid.NewGuid(),
        FullName = fullName.Trim(),
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant(),
        IsActive = true,
        CreatedAtUtc = DateTimeOffset.UtcNow,
    };

    public void Update(string fullName, string? phone, string? email, bool isActive)
    {
        FullName = fullName.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        IsActive = isActive;
    }
}
