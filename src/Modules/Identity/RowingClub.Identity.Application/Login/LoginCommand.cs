using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Login;

public sealed record LoginCommand(string Email, string Password, string? DeviceInfo) : ICommand<LoginResponse>;

public sealed record LoginResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAtUtc, string RefreshToken);
