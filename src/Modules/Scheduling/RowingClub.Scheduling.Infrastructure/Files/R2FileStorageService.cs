using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using RowingClub.Scheduling.Application.Files;

namespace RowingClub.Scheduling.Infrastructure.Files;

/// <summary>
/// Dosyaları Cloudflare R2'ye (S3 uyumlu API) yazar; döndürülen değer, R2 bucket'ına bağlanmış
/// herkese açık alan adı üzerinden TAM (mutlak) bir URL'dir - <see cref="LocalFileStorageService"/>'in
/// döndürdüğü göreli "/uploads/..." yolunun aksine. Frontend bu farkı `lib/api.ts#resolveImageUrl`
/// ile ele alır: "http" ile başlayan yolu olduğu gibi kullanır, başlamayanın önüne kendi
/// `apiBaseUrl`'ini ekler - bu yüzden hangi depolama arka ucu aktifse ekran değişikliği gerekmez.
/// </summary>
public sealed class R2FileStorageService(IAmazonS3 s3Client, IOptions<R2StorageOptions> options) : IFileStorageService
{
    private readonly R2StorageOptions _options = options.Value;

    public async Task<string> SaveAsync(Stream content, string fileName, string subfolder, CancellationToken cancellationToken)
    {
        var key = $"{subfolder}/{fileName}".Replace('\\', '/');

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = content,
            AutoCloseStream = false,
            ContentType = ResolveContentType(fileName),
            // R2, AWS SDK'nın varsayılan akışlı/parçalı gövde imzalamasını (chunked,
            // STREAMING-AWS4-HMAC-SHA256-PAYLOAD[-TRAILER]) hiçbir biçimde desteklemiyor - istek
            // içeriği tek seferde standart AWS4-HMAC-SHA256 ile imzalanmalı.
            UseChunkEncoding = false,
        };

        await s3Client.PutObjectAsync(request, cancellationToken);

        return $"{_options.PublicBaseUrl.TrimEnd('/')}/{key}";
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken)
    {
        var prefix = _options.PublicBaseUrl.TrimEnd('/') + "/";
        if (!path.StartsWith(prefix, StringComparison.Ordinal))
            return; // Bu depolama arka ucuna ait değil (ör. yerel diskten R2'ye geçiş öncesi eski kayıt).

        var key = path[prefix.Length..];
        await s3Client.DeleteObjectAsync(_options.BucketName, key, cancellationToken);
    }

    private static string ResolveContentType(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => "image/jpeg",
    };
}
