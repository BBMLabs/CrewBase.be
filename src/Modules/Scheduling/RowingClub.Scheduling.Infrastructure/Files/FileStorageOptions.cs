namespace RowingClub.Scheduling.Infrastructure.Files;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>Yüklenen dosyaların diskte yazılacağı kök klasör.</summary>
    public string RootPath { get; init; } = "./uploads";
}
