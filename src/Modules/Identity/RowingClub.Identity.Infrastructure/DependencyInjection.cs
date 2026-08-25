using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Application.Email;
using RowingClub.Identity.Application.Recaptcha;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Tokens;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Infrastructure.Email;
using RowingClub.Identity.Infrastructure.Recaptcha;
using RowingClub.Identity.Infrastructure.Repositories;

namespace RowingClub.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICredentialRepository, CredentialRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IRecoveryCodeRepository, RecoveryCodeRepository>();
        services.AddScoped<IPendingTwoFactorTokenRepository, PendingTwoFactorTokenRepository>();

        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection(SmtpOptions.SectionName))
            .ValidateOnStart();

        services.AddHttpClient<IRecaptchaVerifier, GoogleRecaptchaVerifier>();
        services.AddOptions<RecaptchaOptions>()
            .Bind(configuration.GetSection(RecaptchaOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton(new PersistenceAssemblyMarker(typeof(DependencyInjection).Assembly));

        return services;
    }
}
