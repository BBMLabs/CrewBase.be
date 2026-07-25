using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Application.Tokens;

namespace RowingClub.Identity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<IdentityOptions>(configuration.GetSection(IdentityOptions.SectionName));
        services.AddScoped<TokenPairIssuer>();
        services.AddScoped<IAuditLogger, AuditLogger>();

        return services;
    }
}
