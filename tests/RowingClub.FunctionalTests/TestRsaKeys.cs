using System.Security.Cryptography;

namespace RowingClub.FunctionalTests;

/// <summary>Generated once per test run - never a real key, just enough for JwtOptions to pass
/// startup validation and for the auth pipeline to actually issue/validate tokens.</summary>
internal static class TestRsaKeys
{
    private static readonly RSA Rsa = RSA.Create(2048);

    public static string PrivateKeyPem { get; } = Rsa.ExportRSAPrivateKeyPem();

    public static string PublicKeyPem { get; } = Rsa.ExportRSAPublicKeyPem();
}
