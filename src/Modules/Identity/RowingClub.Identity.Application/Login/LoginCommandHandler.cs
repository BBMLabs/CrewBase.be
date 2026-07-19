using MediatR;
using Microsoft.Extensions.Options;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Passwords;
using RowingClub.Identity.Application.Tokens;
using RowingClub.Identity.Domain.Tokens;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.Identity.Application.Login;

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    ICredentialRepository credentialRepository,
    IUserSessionRepository userSessionRepository,
    IPasswordHasher passwordHasher,
    TokenPairIssuer tokenPairIssuer,
    IOptions<IdentityOptions> identityOptions)
    : IRequestHandler<LoginCommand, LoginResponse>
{
    private static readonly AuthenticationFailedException InvalidCredentials =
        new("E-posta veya parola hatalı.");

    private readonly IdentityOptions _options = identityOptions.Value;

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(request.Email);

        var user = await userRepository.GetByEmailAsync(email, cancellationToken)
            ?? throw InvalidCredentials;

        user.EnsureCanAuthenticate();

        var credential = await credentialRepository.GetByUserIdAsync(user.Id, cancellationToken)
            ?? throw InvalidCredentials;

        if (!passwordHasher.Verify(request.Password, credential.PasswordHash))
        {
            user.RegisterFailedLogin(_options.MaxFailedLoginAttempts, _options.LockoutDuration);
            throw InvalidCredentials;
        }

        user.RegisterSuccessfulLogin();

        var (tokenPair, refreshToken) = tokenPairIssuer.IssueNewFamily(user.Id, user.Email.Value);

        userSessionRepository.Add(UserSession.Start(user.Id, refreshToken.FamilyId, request.DeviceInfo));

        return new LoginResponse(tokenPair.AccessToken, tokenPair.AccessTokenExpiresAtUtc, tokenPair.RefreshToken);
    }
}
