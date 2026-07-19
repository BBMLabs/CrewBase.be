using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Refresh;

public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<RefreshTokenResponse>;

public sealed record RefreshTokenResponse(
    string AccessToken, DateTimeOffset AccessTokenExpiresAtUtc, string RefreshToken);
