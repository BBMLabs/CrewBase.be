namespace RowingClub.Scheduling.Application.Members;

public sealed record MemberDto(
    Guid CustomerId,
    string FullName,
    string Phone,
    string? Email,
    int Level,
    string LevelLabel,
    string? MemberCode,
    bool EmailVerified,
    bool PhoneVerified,
    int? DefaultReminderMinutes,
    DateTimeOffset CreatedAtUtc);

/// <summary>Üye log ekranlarında görünen olay adları.</summary>
public static class MemberEvents
{
    public const string Registered = "MEMBER_REGISTERED";
    public const string Login = "MEMBER_LOGIN";
    public const string ProfileUpdated = "PROFILE_UPDATED";
    public const string AccountDeleted = "ACCOUNT_DELETED";
    public const string AppointmentBooked = "APPOINTMENT_BOOKED";
    public const string AppointmentCancelled = "APPOINTMENT_CANCELLED";
    public const string AppointmentCancelledByRsvp = "APPOINTMENT_CANCELLED_RSVP";
    public const string PackageAssigned = "PACKAGE_ASSIGNED";
    public const string PackagePurchased = "PACKAGE_PURCHASED";
    public const string PackageDeducted = "PACKAGE_DEDUCTED";
    public const string PackageRefunded = "PACKAGE_REFUNDED";
    public const string PackageCreditKept = "PACKAGE_CREDIT_KEPT";
    public const string PackageExpired = "PACKAGE_EXPIRED";
    public const string LevelChanged = "LEVEL_CHANGED";
}

public static class NameMask
{
    /// <summary>"Veli Demir" -> "Veli D." : tekne arkadaşları tam adla değil maskeli gösterilir (KVKK).</summary>
    public static string Mask(string fullName)
    {
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return "-";
        if (parts.Length == 1)
            return parts[0];

        return $"{string.Join(' ', parts[..^1])} {char.ToUpperInvariant(parts[^1][0])}.";
    }
}
