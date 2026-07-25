using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.PasswordReset;

public sealed record ForgotPasswordCommand(string Email) : ICommand<Unit>;
