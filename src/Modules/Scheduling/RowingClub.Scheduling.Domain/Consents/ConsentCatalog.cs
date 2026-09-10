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
            "Yüzmeyi bildiğimi ve suda güvenli hareket edebileceğimi beyan ederim. Kürek sporunda su üzerinde antrenman yapmam gerektiğini ve yüzme bilgimin önemli olduğunu kabul ediyorum. " +
            "Bu beyan bir kişisel veri işleme rızası değil, güvenliğiniz için alınan bir taahhüttür: yalnızca 'onaylandı' bilgisi, onay tarihiyle birlikte kulübün kendi veritabanında saklanır " +
            "ve kimseyle paylaşılmaz. Beyanınızın gerçeği yansıtmaması durumunda oluşabilecek kazalardan kulüp sorumlu tutulamaz.",
            ConsentScope.Booking, Required: true, Icon: "🏊"),
        new("health", "Sağlık Beyanı",
            "Kürek sporuna engel teşkil edecek bir sağlık sorunum olmadığını beyan ederim. Sağlık durumumda değişiklik olması halinde derhal kulüp yönetimini bilgilendireceğimi taahhüt ederim. " +
            "Bu beyan bir güvenlik taahhüdüdür; yalnızca 'onaylandı' bilgisi tarihiyle birlikte kulübün kendi veritabanında saklanır, üçüncü bir kişi veya kurumla paylaşılmaz. " +
            "Gerçek sağlık durumunuzu ayrıntılı olarak bildirmek isterseniz bu, ayrı ve isteğe bağlı 'Sağlık Verisi Açık Rıza' beyanı kapsamındadır.",
            ConsentScope.Booking, Required: true, Icon: "❤"),
        new("rules", "Kürek Kulübü ve Rezervasyon Kuralları",
            "Kürek kulübü ve rezervasyon kurallarını okudum, anladım ve kabul ediyorum. Kurallara uygun hareket edeceğimi, kulüp ekipmanına özenli davranacağımı ve rezervasyon " +
            "iptal/gecikme kurallarına uyacağımı taahhüt ederim. Bu onay bir kişisel veri işleme rızası değil, kulüple aranızdaki hizmet şartlarının kabulüdür; yalnızca onay bilgisi " +
            "tarihiyle birlikte saklanır ve üçüncü kişilerle paylaşılmaz.",
            ConsentScope.Booking, Required: true, Icon: "📋"),
        new("kvkk", "Kişisel Verilerin İşlenmesi Aydınlatma Metni",
            "6698 sayılı Kişisel Verilerin Korunması Kanunu ('KVKK') uyarınca veri sorumlusu, hizmet aldığınız kulüptür. Randevu/üyelik sürecinde ad-soyad, telefon, e-posta ve randevu " +
            "geçmişi gibi kişisel verileriniz; randevu ve üyelik süreçlerinin yürütülmesi, sizinle iletişim kurulması, kulüp içi seviye/derece takibi ve yasal yükümlülüklerin yerine " +
            "getirilmesi amacıyla (KVKK m.5) işlenir. Verileriniz kulübe özel, şifrelenmiş bir veritabanında saklanır; yalnızca yasal zorunluluk hâlleri dışında hiçbir üçüncü kişi, " +
            "kurum veya şirketle paylaşılmaz, satılmaz ya da kiralanmaz. Verileriniz üyeliğiniz/kayıtlarınız süresince ve yasal saklama süreleri boyunca tutulur, süre sonunda silinir " +
            "veya anonim hâle getirilir. KVKK m.11 uyarınca verilerinizin işlenip işlenmediğini öğrenme, işlenmişse buna ilişkin bilgi talep etme, düzeltilmesini veya silinmesini isteme " +
            "ve işlemeye itiraz etme haklarına sahipsiniz; bu talepleri kulübün iletişim bilgileri üzerinden iletebilirsiniz. Bu aydınlatma metnini okuduğumu ve kişisel verilerimin " +
            "yukarıda açıklanan şekilde işlenmesine özgür irademle onay verdiğimi beyan ederim.",
            ConsentScope.Booking, Required: true, Icon: "🔒"),
        new("health-data", "Sağlık Verisi Açık Rıza",
            "KVKK m.6 uyarınca sağlık verisi özel nitelikli kişisel veri sayılır ve ancak açık rızanızla işlenebilir. Paylaştığınız sağlık bilgileri (varsa kronik rahatsızlık, alerji, " +
            "kullanılan ilaç gibi antrenman güvenliğinizle ilgili bilgiler) yalnızca antrenman sırasında güvenliğinizin sağlanması amacıyla, kulübe özel şifrelenmiş veritabanında " +
            "saklanır; sigorta şirketleri dâhil hiçbir üçüncü kişi veya kurumla paylaşılmaz, pazarlama amacıyla kullanılmaz. Bu rıza isteğe bağlıdır; üye panelinizden dilediğiniz zaman " +
            "geri çekebilirsiniz, ancak geri çekmeniz durumunda kulüp sağlık durumunuzu bilmeden hizmet vereceğinden doğabilecek riskler size ait olur. Verileriniz üyeliğiniz sona " +
            "erdikten sonra yasal saklama süresi kadar tutulur ve ardından kalıcı olarak silinir.",
            ConsentScope.Member, Required: true, Icon: "🏥"),
        new("photo", "Fotoğraf Açık Rıza",
            "Antrenman ve etkinlikler sırasında çekilebilecek fotoğraf/videolarınızın, kulübün kendi web sitesi ve sosyal medya hesaplarında tanıtım amacıyla kullanılmasına onay " +
            "verip vermediğinizi belirtir. Görselleriniz ticari amaçla üçüncü kişi veya kurumlara satılmaz ya da onlarla paylaşılmaz; yalnızca kulübün kendi tanıtım kanallarında " +
            "kullanılır. Bu rıza tamamen isteğe bağlıdır ve üye panelinizden dilediğiniz zaman geri çekebilirsiniz; geri çektiğinizde yeni paylaşımlarda görselleriniz kullanılmaz " +
            "ve talebiniz üzerine mevcut yayındaki görseller makul bir sürede kaldırılır.",
            ConsentScope.Member, Required: false, Icon: "📷"),
        new("marketing", "Ticari İleti (E-posta/SMS)",
            "Kulübün kampanya, duyuru ve bilgilendirme amaçlı ticari elektronik iletilerini (e-posta/SMS) e-posta adresinize veya telefon numaranıza göndermesine onay verip " +
            "vermediğinizi belirtir. İletişim bilgileriniz yalnızca bu amaçla kullanılır, hiçbir üçüncü kişi veya kurumla paylaşılmaz ya da başka bir şirkete pazarlama amacıyla " +
            "aktarılmaz. Bu rıza tamamen isteğe bağlıdır; dilediğiniz zaman üye panelinizden veya gönderilen iletideki ret bağlantısı üzerinden geri çekebilirsiniz, geri çekmeniz " +
            "randevu/üyelik hizmetlerinizi etkilemez.",
            ConsentScope.Member, Required: false, Icon: "📧"),
    ];

    public static IEnumerable<ConsentDefinition> BookingConsents =>
        All.Where(c => c.Scope == ConsentScope.Booking);

    public static IEnumerable<ConsentDefinition> MemberConsents =>
        All.Where(c => c.Scope == ConsentScope.Member);

    public static ConsentDefinition? Find(string key) =>
        All.FirstOrDefault(c => c.Key == key);
}
