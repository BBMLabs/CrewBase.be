namespace RowingClub.Scheduling.Application.Files;

/// <summary>
/// Küçük ikili dosyaları (paket görselleri gibi) kalıcı depoya yazar ve tarayıcının doğrudan
/// erişebileceği bir URL döner - yerel disk aktifse "/uploads/..." ile başlayan GÖRELİ bir yol,
/// Cloudflare R2 aktifse MUTLAK bir URL (bkz. LocalFileStorageService/R2FileStorageService;
/// frontend farkı `lib/api.ts#resolveImageUrl` ile ele alır).
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// <paramref name="subfolder"/> KLASÖR KURALI: depolama (R2/yerel disk) katalog/tenant
    /// veritabanı gibi firma başına izole DEĞİLDİR - tüm firmalar tek ortak bucket/klasörü
    /// paylaşır. Bu yüzden her çağrı, alanı firma alt alan adıyla başlatmalı:
    /// <c>"{firmaSubdomain}/paketler/{packageId}"</c> (paketin okunabilir bir kodu yok, GUID
    /// kullanılır), <c>"{firmaSubdomain}/subeler/{subeKodu}"</c> (şube logosu - GUID DEĞİL,
    /// okunabilir şube kodu), gelecekte üyeye özel bir görsel eklenirse:
    /// <c>"{firmaSubdomain}/subeler/{subeKodu}/uyeler/{uyeKodu}"</c> (yine GUID değil, üye kodu).
    /// Firma segmenti olmadan çağırma - bir başka firmanın dosyasıyla anlamsız şekilde karışır.
    /// </summary>
    Task<string> SaveAsync(Stream content, string fileName, string subfolder, CancellationToken cancellationToken);

    /// <summary>
    /// <paramref name="path"/>, <see cref="SaveAsync"/>'in döndürdüğü değerin AYNISI olmalı (göreli
    /// "/uploads/..." ya da mutlak R2 URL'i) - bir varlık (paket/şube vb.) kalıcı olarak silinirken
    /// görselinin de depodan (yerel disk/R2) temizlenmesi için. Dosya zaten yoksa sessizce döner.
    /// </summary>
    Task DeleteAsync(string path, CancellationToken cancellationToken);
}
