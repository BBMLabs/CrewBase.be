using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.PasswordReset;

public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword) : ICommand<Unit>;
