using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RowingClub.BuildingBlocks.Security.Encryption;
using RowingClub.BuildingBlocks.Security.Jwt;
using RowingClub.BuildingBlocks.Security.Passwords;
using RowingClub.BuildingBlocks.Security.Tokens;

namespace RowingClub.BuildingBlocks.Security;

public static class DependencyInjection
{
    public static IServiceCollection AddRowingClubSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<EncryptionOptions>()
            .Bind(configuration.GetSection(EncryptionOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher, Argon2IdPasswordHasher>();
        services.AddSingleton<IRefreshTokenHasher, Sha256RefreshTokenHasher>();
        services.AddSingleton<IOpaqueTokenGenerator, OpaqueTokenGenerator>();
        services.AddSingleton<IJwtTokenService, RsaJwtTokenService>();
        services.AddSingleton<IFieldEncryptor, AesGcmFieldEncryptor>();
        services.AddSingleton<IBlindIndexer, HmacBlindIndexer>();

        return services;
    }
}
