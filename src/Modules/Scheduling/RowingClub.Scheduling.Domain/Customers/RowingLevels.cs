namespace RowingClub.Scheduling.Domain.Customers;

/// <summary>
/// Kürek deneyim dereceleri (0-10) ve etiketleri. Dereceyi YALNIZCA firma yöneticisi belirler;
/// üye kendi panelinde salt-okunur görür. Seans gruplaması bu derece üzerinden yapılır.
/// </summary>
public static class RowingLevels
{
    public static readonly IReadOnlyList<string> Labels =
    [
        "Hiç çekmedim",
        "Temel eğitimim yeni başladı (1-2. ders)",
        "Temel eğitimim sürüyor (3+ ders)",
        "Temel eğitim tamamlandı",
        "1+ yıldır çekiyorum",
        "Bölgesel yarıştım",
        "Ulusal yarıştım",
        "Ulusal madalya kazandım",
        "Uluslararası yarıştım",
        "Milli takımda yer aldım",
        "Milli takımda madalya kazandım",
    ];

    public static string LabelOf(int level) =>
        level >= 0 && level < Labels.Count ? Labels[level] : level.ToString();
}
