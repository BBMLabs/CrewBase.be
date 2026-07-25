using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Login;

public sealed record VerifyTwoFactorLoginCommand(string PendingToken, string Code) : ICommand<VerifyTwoFactorLoginResponse>;

public sealed record VerifyTwoFactorLoginResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    int? RemainingRecoveryCodes = null);
