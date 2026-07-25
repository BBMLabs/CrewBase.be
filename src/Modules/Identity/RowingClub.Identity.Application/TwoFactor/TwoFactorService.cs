using OtpNet;
using QRCoder;

namespace RowingClub.Identity.Application.TwoFactor;

public static class TwoFactorService
{
    public static string GenerateTotpSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    public static bool VerifyTotpCode(string secret, string code)
    {
        var key = Base32Encoding.ToBytes(secret);
        var totp = new Totp(key, step: 30, mode: OtpHashMode.Sha256, totpSize: 6);
        return totp.VerifyTotp(code, out _);
    }

    public static string GenerateTotpQrCode(string secret, string email, string issuer = "RowingClub")
    {
        var uri = new OtpUri(OtpType.Totp, secret, email, issuer);
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(uri.ToString(), QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        var qrBytes = qrCode.GetGraphic(4);
        return Convert.ToBase64String(qrBytes);
    }

    public static string GenerateEmailOtpCode()
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(4);
        var code = (Math.Abs(BitConverter.ToInt32(bytes)) % 900000 + 100000).ToString();
        return code;
    }
}
