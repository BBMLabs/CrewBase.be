using Microsoft.Extensions.Options;
using RowingClub.Scheduling.Application.Files;

namespace RowingClub.Scheduling.Infrastructure.Files;

/// <summary>
/// Dosyaları yerel diske yazar (RootPath/subfolder/fileName). Program.cs'teki
/// app.UseStaticFiles(...) aynı RootPath'i "/uploads" altında sunar - bu yüzden döndürülen yol
/// doğrudan tarayıcıdan erişilebilir.
/// </summary>
public sealed class LocalFileStorageService(IOptions<FileStorageOptions> options) : IFileStorageService
{
    public async Task<string> SaveAsync(Stream content, string fileName, string subfolder, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(options.Value.RootPath, subfolder);
        Directory.CreateDirectory(directory);

        var fullPath = Path.Combine(directory, fileName);
        await using (var fileStream = File.Create(fullPath))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        return $"/uploads/{subfolder}/{fileName}".Replace('\\', '/');
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken)
    {
        const string prefix = "/uploads/";
        if (!path.StartsWith(prefix, StringComparison.Ordinal))
            return Task.CompletedTask;

        var relative = path[prefix.Length..].Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(options.Value.RootPath, relative);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }
}
