using MediatR;
using Microsoft.Extensions.Options;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Passwords;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Application.Recaptcha;
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
    IOptions<IdentityOptions> identityOptions,
    IAuditLogger auditLogger,
    IRecaptchaVerifier recaptchaVerifier)
    : IRequestHandler<LoginCommand, LoginResult>
{
    private static readonly AuthenticationFailedException InvalidCredentials =
        new("E-posta veya parola hatalı.");

    private static readonly AuthenticationFailedException RecaptchaFailed =
        new("Doğrulama başarısız oldu, lütfen tekrar deneyin.");

    private readonly IdentityOptions _options = identityOptions.Value;

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        if (!await recaptchaVerifier.VerifyAsync(request.RecaptchaToken, "login", cancellationToken))
        {
            auditLogger.Log("LOGIN_RECAPTCHA_FAILED", request.Email, "reCAPTCHA doğrulaması başarısız.");
            throw RecaptchaFailed;
        }

        var email = EmailAddress.Create(request.Email);

        var user = await userRepository.GetByEmailAsync(email, cancellationToken)
            ?? throw InvalidCredentials;

        user.EnsureCanAuthenticate();

        var credential = await credentialRepository.GetByUserIdAsync(user.Id, cancellationToken)
            ?? throw InvalidCredentials;

        if (!passwordHasher.Verify(request.Password, credential.PasswordHash))
        {
            user.RegisterFailedLogin(_options.MaxFailedLoginAttempts, _options.LockoutDuration);
            auditLogger.Log("LOGIN_FAILED", user.Id.ToString(), $"Başarısız giriş denemesi. UA: {request.DeviceInfo}");
            throw InvalidCredentials;
        }

        user.RegisterSuccessfulLogin();
        auditLogger.Log("LOGIN_SUCCESS", user.Id.ToString(), "Başarılı giriş.");

        var role = user.Role.ToString();
        var (tokenPair, refreshToken) = tokenPairIssuer.IssueNewFamily(user.Id, user.Email.Value, role, user.CompanyId);

        userSessionRepository.Add(UserSession.Start(user.Id, refreshToken.FamilyId, request.DeviceInfo));

        return LoginResult.Complete(tokenPair.AccessToken, tokenPair.AccessTokenExpiresAtUtc, tokenPair.RefreshToken);
    }
}
