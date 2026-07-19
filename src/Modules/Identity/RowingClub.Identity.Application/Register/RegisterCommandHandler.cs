using MediatR;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Passwords;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.Identity.Application.Register;

public sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    ICredentialRepository credentialRepository,
    IPasswordHasher passwordHasher)
    : IRequestHandler<RegisterCommand, RegisterResponse>
{
    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(request.Email);

        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            throw new DomainException("email_already_registered", "Bu e-posta adresi zaten kayıtlı.");
        }

        var user = User.Register(email);
        var credential = Credential.Create(user.Id, passwordHasher.Hash(request.Password));

        userRepository.Add(user);
        credentialRepository.Add(credential);

        return new RegisterResponse(user.Id, user.Email.Value, user.CreatedAtUtc);
    }
}
