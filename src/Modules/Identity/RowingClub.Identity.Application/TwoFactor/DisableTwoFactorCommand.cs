using MediatR;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Domain.Users;

namespace RowingClub.Identity.Application.TwoFactor;

public sealed record DisableTwoFactorCommand(string Password) : ICommand<Unit>;

public sealed class DisableTwoFactorCommandHandler(
    ICurrentUser currentUser,
    IUserRepository userRepository,
    ICredentialRepository credentialRepository,
    BuildingBlocks.Security.Passwords.IPasswordHasher passwordHasher)
    : IRequestHandler<DisableTwoFactorCommand, Unit>
{
    public async Task<Unit> Handle(DisableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new DomainException("user_not_found", "Kullanıcı bulunamadı.");

        var credential = await credentialRepository.GetByUserIdAsync(user.Id, cancellationToken)
            ?? throw new DomainException("user_not_found", "Kullanıcı bulunamadı.");

        if (!passwordHasher.Verify(request.Password, credential.PasswordHash))
            throw new DomainException("invalid_password", "Parola hatalı.");

        user.DisableTwoFactor();

        return Unit.Value;
    }
}
