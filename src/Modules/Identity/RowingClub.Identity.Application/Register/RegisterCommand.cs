using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Register;

public sealed record RegisterCommand(string Email, string Password) : ICommand<RegisterResponse>;

public sealed record RegisterResponse(Guid UserId, string Email, DateTimeOffset CreatedAtUtc);
