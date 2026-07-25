using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.EmailVerification;

public sealed record VerifyEmailCommand(string Email, string Token) : ICommand<Unit>;
