using MediatR;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.Identity.Domain.Users;

namespace RowingClub.Identity.Application.TwoFactor;

public sealed record SetupTotpQuery : IQuery<SetupTotpResponse>;

public sealed record SetupTotpResponse(string Secret, string QrCodeBase64);

public sealed class SetupTotpQueryHandler(
    ICurrentUser currentUser,
    IUserRepository userRepository)
    : IRequestHandler<SetupTotpQuery, SetupTotpResponse>
{
    public async Task<SetupTotpResponse> Handle(SetupTotpQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Kullanıcı bulunamadı.");

        var secret = TwoFactorService.GenerateTotpSecret();
        var qrCode = TwoFactorService.GenerateTotpQrCode(secret, user.Email.Value);

        return new SetupTotpResponse(secret, qrCode);
    }
}
