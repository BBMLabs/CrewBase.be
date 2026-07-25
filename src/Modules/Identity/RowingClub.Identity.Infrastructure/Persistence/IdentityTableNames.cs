namespace RowingClub.Identity.Infrastructure.Persistence;

public static class IdentityTableNames
{
    public const string Users = "identity_users";
    public const string Credentials = "identity_credentials";
    public const string RefreshTokens = "identity_refresh_tokens";
    public const string UserSessions = "identity_user_sessions";
    public const string EmailVerificationTokens = "identity_email_verification_tokens";
    public const string PasswordResetTokens = "identity_password_reset_tokens";
}
