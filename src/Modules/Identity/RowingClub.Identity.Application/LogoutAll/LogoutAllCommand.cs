using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.LogoutAll;

/// <summary>Revokes every active session/refresh-token family for the authenticated caller
/// (spec section 11 - "tüm cihazlardan çıkış").</summary>
public sealed record LogoutAllCommand : ICommand<Unit>;
