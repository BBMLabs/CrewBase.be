using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.TwoFactor.GenerateRecoveryCodes;

public sealed record GenerateRecoveryCodesCommand(Guid UserId) : ICommand<GenerateRecoveryCodesResponse>;

public sealed record GenerateRecoveryCodesResponse(IReadOnlyCollection<string> RecoveryCodes);
