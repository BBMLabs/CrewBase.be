using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.TwoFactor.VerifyRecoveryCode;

public sealed record VerifyRecoveryCodeCommand(Guid UserId, string RecoveryCode) : ICommand<Unit>;
