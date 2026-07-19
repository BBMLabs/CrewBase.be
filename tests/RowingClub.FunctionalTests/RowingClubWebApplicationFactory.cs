using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace RowingClub.FunctionalTests;

/// <summary>
/// Boots the real <c>RowingClub.Api</c> pipeline with throwaway-but-valid secrets, and strips the
/// Mongo migration hosted service so the host can start without a live database - see
/// RowingClub.SecurityTests.RowingClubWebApplicationFactory for the identical rationale.
/// </summary>
public sealed class RowingClubWebApplicationFactory : WebApplicationFactory<RowingClub.Api.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:Issuer", "https://rowingclub.tests");
        builder.UseSetting("Jwt:Audience", "rowingclub-tests");
        builder.UseSetting("Jwt:KeyId", "1");
        builder.UseSetting("Jwt:SigningPrivateKeyPem", TestRsaKeys.PrivateKeyPem);
        builder.UseSetting("Jwt:SigningPublicKeyPem", TestRsaKeys.PublicKeyPem);

        builder.UseSetting("FieldEncryption:CurrentKeyVersion", "1");
        builder.UseSetting("FieldEncryption:Keys:1", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
        builder.UseSetting("FieldEncryption:BlindIndexKey", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");

        builder.UseSetting("Mongo:ConnectionString", "mongodb://unused:27017/unused?replicaSet=rs0");
        builder.UseSetting("Mongo:DatabaseName", "unused");
        builder.UseSetting("ConnectionStrings:Redis", "unused:6379,abortConnect=false");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();
        });
    }
}
