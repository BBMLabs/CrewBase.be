using MediatR;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Identity.Domain.Users;

namespace RowingClub.Identity.Application.TwoFactor.GenerateRecoveryCodes;

public sealed class GenerateRecoveryCodesCommandHandler(
    IRecoveryCodeRepository recoveryCodeRepository,
    IOpaqueTokenGenerator opaqueTokenGenerator,
    IRefreshTokenHasher refreshTokenHasher)
    : IRequestHandler<GenerateRecoveryCodesCommand, GenerateRecoveryCodesResponse>
{
    private const int CodeCount = 10;

    public async Task<GenerateRecoveryCodesResponse> Handle(
        GenerateRecoveryCodesCommand request, CancellationToken cancellationToken)
    {
        var existing = await recoveryCodeRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        foreach (var code in existing)
        {
            code.MarkUsed();
            recoveryCodeRepository.Update(code);
        }

        var codes = new List<string>(CodeCount);
        var entities = new List<UserRecoveryCode>(CodeCount);

        foreach (var _ in Enumerable.Range(0, CodeCount))
        {
            var raw = opaqueTokenGenerator.Generate();
            var hash = refreshTokenHasher.Hash(raw);
            codes.Add(raw);
            entities.Add(UserRecoveryCode.Create(request.UserId, hash));
        }

        recoveryCodeRepository.AddRange(entities);

        return new GenerateRecoveryCodesResponse(codes.AsReadOnly());
    }
}
