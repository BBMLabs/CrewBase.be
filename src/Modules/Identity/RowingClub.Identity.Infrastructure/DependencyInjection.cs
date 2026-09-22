using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Application.Billing;
using RowingClub.Identity.Application.Email;
using RowingClub.Identity.Application.Recaptcha;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Companies.Billing;
using RowingClub.Identity.Domain.Platform;
using RowingClub.Identity.Domain.Tokens;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Infrastructure.Billing.Iyzico;
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
        services.AddScoped<ICompanyGalleryImageRepository, CompanyGalleryImageRepository>();
        services.AddScoped<ICompanySubscriptionRepository, CompanySubscriptionRepository>();
        services.AddScoped<ICompanyPaymentRepository, CompanyPaymentRepository>();
        services.AddScoped<IRecoveryCodeRepository, RecoveryCodeRepository>();
        services.AddScoped<IPendingTwoFactorTokenRepository, PendingTwoFactorTokenRepository>();
        services.AddScoped<IPlatformActivityLogRepository, PlatformActivityLogRepository>();
        services.AddScoped<IPlatformActivityLogWriter, PlatformActivityLogWriter>();

        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection(SmtpOptions.SectionName))
            .ValidateOnStart();

        services.AddHttpClient<IRecaptchaVerifier, GoogleRecaptchaVerifier>(
                c => c.Timeout = TimeSpan.FromSeconds(10))
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { ConnectCallback = Ipv4PreferringConnectCallback });
        services.AddOptions<RecaptchaOptions>()
            .Bind(configuration.GetSection(RecaptchaOptions.SectionName))
            .ValidateOnStart();

        services.AddHttpClient<IIyzicoSubscriptionClient, IyzicoSubscriptionClient>(
            c => c.Timeout = TimeSpan.FromSeconds(15));
        services.AddOptions<IyzicoOptions>()
            .Bind(configuration.GetSection(IyzicoOptions.SectionName));

        services.AddSingleton(new PersistenceAssemblyMarker(typeof(DependencyInjection).Assembly));

        return services;
    }

    private static async ValueTask<Stream> Ipv4PreferringConnectCallback(
        SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
        var orderedAddresses = addresses.OrderBy(a => a.AddressFamily == AddressFamily.InterNetwork ? 0 : 1);

        Exception? lastConnectError = null;
        foreach (var address in orderedAddresses)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(address, context.DnsEndPoint.Port, cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (Exception ex)
            {
                lastConnectError = ex;
                socket.Dispose();
            }
        }

        throw lastConnectError ?? new SocketException((int)SocketError.HostUnreachable);
    }
}
