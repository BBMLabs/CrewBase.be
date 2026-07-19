namespace RowingClub.Identity.Application;

/// <summary>Identity module's own business policy knobs (spec section 11 - brute-force koruması, hesap kilitleme).</summary>
public sealed class IdentityOptions
{
    public const string SectionName = "Identity";

    public int MaxFailedLoginAttempts { get; init; } = 5;

    public TimeSpan LockoutDuration { get; init; } = TimeSpan.FromMinutes(15);

    public TimeSpan RefreshTokenLifetime { get; init; } = TimeSpan.FromDays(30);
}
