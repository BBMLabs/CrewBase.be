using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Security.Jwt;
using System.Security.Cryptography;

namespace RowingClub.Api.Security;

public static class AuthenticationSetup
{
    public static IServiceCollection AddRowingClubAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddScoped<ICurrentTenant, HttpContextCurrentTenant>();

        var jwtSection = configuration.GetSection(JwtOptions.SectionName);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Keep raw claim types ("sub", "email", ...) instead of ASP.NET Core's legacy
                // ClaimTypes.* remapping, so they match exactly what RsaJwtTokenService issues.
                options.MapInboundClaims = false;

                var publicKey = RSA.Create();
                publicKey.ImportFromPem(jwtSection["SigningPublicKeyPem"]);

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSection["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(publicKey),
                };
            });

        services.AddAuthorization();

        return services;
    }
}
