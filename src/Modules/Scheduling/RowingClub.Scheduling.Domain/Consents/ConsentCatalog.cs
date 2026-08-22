namespace RowingClub.Scheduling.Domain.Consents;

public enum ConsentScope
{
    /// <summary>Randevu almak için gereken beyanlar: misafir HER randevuda, üye BİR KEZ onaylar.</summary>
    Booking = 0,

    /// <summary>Üyelik için gereken beyanlar: üye kaydında/panelinde bir kez onaylanır.</summary>
    Member = 1,
}

public sealed record ConsentDefinition(
    string Key, string Title, string Body, ConsentScope Scope, bool Required, string Icon);

/// <summary>
/// Platform genelinde tanımlı beyan/rıza metinleri. İki gruba ayrılır: randevu için gerekenler
/// (yüzme, sağlık, kurallar, KVKK) ve üyelik için gerekenler (sağlık verisi, fotoğraf, ticari
/// ileti). Fotoğraf ve ticari ileti isteğe bağlıdır; reddedilebilir ve sonradan değiştirilebilir.
/// </summary>
public static class ConsentCatalog
{
    public static readonly IReadOnlyList<ConsentDefinition> All =
    [
        new("swim", "Yüzme Beyanı",
            "Yüzmeyi bildiğimi ve suda güvenli hareket edebileceğimi beyan ederim. Kürek sporunda su üzerinde antrenman yapmam gerektiğini ve yüzme bilgimin önemli olduğunu kabul ediyorum.",
            ConsentScope.Booking, Required: true, Icon: "🏊"),
        new("health", "Sağlık Beyanı",
            "Kürek sporuna engel teşkil edecek bir sağlık sorunum olmadığını beyan ederim. Sağlık durumumda değişiklik olması halinde derhal kulüp yönetimini bilgilendireceğimi taahhüt ederim.",
            ConsentScope.Booking, Required: true, Icon: "❤"),
        new("rules", "Kürek Kulübü ve Rezervasyon Kuralları",
            "Kürek kulübü ve rezervasyon kurallarını okudum, anladım ve kabul ediyorum. Kurallara uygun hareket edeceğimi taahhüt ederim.",
            ConsentScope.Booking, Required: true, Icon: "📋"),
        new("kvkk", "Kişisel Verilerin İşlenmesi Aydınlatma Metni",
            "Aydınlatma metnini okudum ve kişisel verilerimin işlenmesine özgür irademle onay veriyorum.",
            ConsentScope.Booking, Required: true, Icon: "🔒"),
        new("health-data", "Sağlık Verisi Açık Rıza",
            "Aydınlatma metnini okudum ve sağlık verilerimin işlenmesine özgür irademle onay veriyorum.",
            ConsentScope.Member, Required: true, Icon: "🏥"),
        new("photo", "Fotoğraf Açık Rıza",
            "Aydınlatma metnini okudum ve fotoğraf verilerimin işlenmesine özgür irademle onay veriyorum.",
            ConsentScope.Member, Required: false, Icon: "📷"),
        new("marketing", "Ticari İleti (E-posta/SMS)",
            "Aydınlatma metnini okudum ve ticari ileti (e-posta/SMS) verilerimin işlenmesine özgür irademle onay veriyorum.",
            ConsentScope.Member, Required: false, Icon: "📧"),
    ];

    public static IEnumerable<ConsentDefinition> BookingConsents =>
        All.Where(c => c.Scope == ConsentScope.Booking);

    public static IEnumerable<ConsentDefinition> MemberConsents =>
        All.Where(c => c.Scope == ConsentScope.Member);

    public static ConsentDefinition? Find(string key) =>
        All.FirstOrDefault(c => c.Key == key);
}
