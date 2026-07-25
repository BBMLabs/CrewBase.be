using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.EmailVerification;

public sealed record SendVerificationEmailCommand(string Email) : ICommand<Unit>;
