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
    // Must run before Program.cs's top-level code (DotEnvFileLoader) does, so a developer's real
    // .env.developer on disk never leaks into this test run and overrides the settings below.
    // A static constructor is guaranteed to run before any member access on this type, including
    // the instance constructor IClassFixture uses well before CreateClient() builds the host.
    static RowingClubWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable(
            RowingClub.Api.Configuration.DotEnvFileLoader.DisableEnvVarName, "1");
    }

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

        builder.UseSetting("Mongo:Host", "unused");
        builder.UseSetting("Mongo:Port", "27017");
        builder.UseSetting("Mongo:DatabaseName", "unused");
        builder.UseSetting("Redis:Host", "unused");
        builder.UseSetting("Redis:Port", "6379");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();
        });
    }
}
