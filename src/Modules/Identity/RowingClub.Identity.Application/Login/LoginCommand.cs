using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Login;

public sealed record LoginCommand(string Email, string Password, string? DeviceInfo) : ICommand<LoginResult>;

public sealed record LoginResult(
    bool RequiresTwoFactor,
    string? PendingTwoFactorToken,
    string? AccessToken,
    DateTimeOffset? AccessTokenExpiresAtUtc,
    string? RefreshToken)
{
    public static LoginResult Complete(string accessToken, DateTimeOffset expiresAt, string refreshToken) =>
        new(false, null, accessToken, expiresAt, refreshToken);

    public static LoginResult TwoFactorRequired(string pendingToken) =>
        new(true, pendingToken, null, null, null);
}
