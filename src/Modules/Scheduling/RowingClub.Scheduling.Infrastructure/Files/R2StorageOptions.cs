namespace RowingClub.Scheduling.Infrastructure.Files;

/// <summary>
/// Cloudflare R2 (S3 uyumlu) nesne depolama yapılandırması. Boş bırakılırsa devre dışı kalır ve
/// <see cref="LocalFileStorageService"/> kullanılmaya devam eder (bkz. DependencyInjection.cs) -
/// aynı IYZICO_*/SMTP_* desenindeki "boşsa devre dışı" davranışı.
/// </summary>
public sealed class R2StorageOptions
{
    public const string SectionName = "R2Storage";

    /// <summary>Cloudflare hesap kimliği - S3 API uç noktasını oluşturur: https://{AccountId}.r2.cloudflarestorage.com</summary>
    public string AccountId { get; init; } = string.Empty;

    /// <summary>R2 API Token'ın (Object Read &amp; Write, bucket'a özel) Access Key Id'si.</summary>
    public string AccessKeyId { get; init; } = string.Empty;

    /// <summary>R2 API Token'ın gizli anahtarı - loglanmaz, koda yazılmaz, yalnızca env'den okunur.</summary>
    public string SecretAccessKey { get; init; } = string.Empty;

    public string BucketName { get; init; } = string.Empty;

    /// <summary>
    /// Bucket'a bağlanmış herkese açık teslim adresi (özel bir alan adı - ör.
    /// "https://cdn.faturebase.com" - ya da geliştirme için r2.dev public URL'i), sonu / OLMADAN.
    /// Dönen yol bu adrese eklenir: {PublicBaseUrl}/{subfolder}/{fileName}.
    /// </summary>
    public string PublicBaseUrl { get; init; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AccountId) &&
        !string.IsNullOrWhiteSpace(AccessKeyId) &&
        !string.IsNullOrWhiteSpace(SecretAccessKey) &&
        !string.IsNullOrWhiteSpace(BucketName) &&
        !string.IsNullOrWhiteSpace(PublicBaseUrl);
}
