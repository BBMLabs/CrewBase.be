using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Logout;

/// <summary>Revokes only the refresh token family the presented token belongs to - i.e. logs out
/// this one device/client, not every session (spec section 11 - "Tek cihazdan çıkış").</summary>
public sealed record LogoutCommand(string RefreshToken) : ICommand<Unit>;
