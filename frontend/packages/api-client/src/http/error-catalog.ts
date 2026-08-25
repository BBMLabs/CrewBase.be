/**
 * Semantic Turkish UX copy for known backend error codes.
 * Source of codes: docs/frontend/API-INVENTORY.md (verified occurrences).
 * Unknown codes fall back to the server message — never silently swallowed.
 */
export interface ErrorCopy {
  readonly title: string;
  readonly description: string;
}

const CATALOG: Readonly<Record<string, ErrorCopy>> = {
  unauthorized: {
    title: "Oturum gerekli",
    description: "Devam etmek için lütfen giriş yapın.",
  },
  forbidden: {
    title: "Yetkiniz yok",
    description: "Bu işlem için yetkiniz bulunmuyor.",
  },
  validation_error: {
    title: "Bilgileri kontrol edin",
    description: "Girdiğiniz bazı bilgiler geçersiz görünüyor.",
  },
  company_not_found: {
    title: "Kulüp bulunamadı",
    description: "Hesabınıza bağlı aktif bir kulüp bulunamadı.",
  },
  company_name_taken: {
    title: "Kulüp adı kullanımda",
    description: "Bu isimle kayıtlı bir kulüp zaten var. Farklı bir ad deneyin.",
  },
  email_already_registered: {
    title: "E-posta kayıtlı",
    description: "Bu e-posta adresi zaten kayıtlı.",
  },
  email_taken: {
    title: "E-posta kayıtlı",
    description: "Bu e-posta adresiyle bir üyelik zaten var.",
  },
  member_exists: {
    title: "Üyelik mevcut",
    description: "Bu telefon numarasıyla bir üyelik zaten var. Giriş yapabilirsiniz.",
  },
  phone_taken: {
    title: "Telefon kayıtlı",
    description: "Bu telefon numarasıyla kayıtlı bir üye zaten var.",
  },
  invalid_date: { title: "Geçersiz tarih", description: "Tarih YYYY-AA-GG biçiminde olmalıdır." },
  invalid_time: { title: "Geçersiz saat", description: "Saat SS:DD biçiminde olmalıdır." },
  invalid_slot: {
    title: "Geçersiz seans saati",
    description: "Seçilen saat çalışma saatleri dışında veya uygun değil.",
  },
  slot_full: {
    title: "Seans dolu",
    description: "Bu saat için uygun kontenjan bulunmamaktadır. Lütfen başka bir saat seçin.",
  },
  session_full: {
    title: "Seans dolu",
    description: "Bu seansta boş koltuk kalmadı.",
  },
  already_booked: {
    title: "Zaten randevunuz var",
    description: "Aynı saat için zaten bir randevunuz bulunuyor.",
  },
  closed_date: {
    title: "Kulüp kapalı",
    description: "Seçilen tarihte kulübümüz randevuya kapalıdır.",
  },
  too_soon: {
    title: "Çok yakın bir tarih",
    description: "Rezervasyon için son başvuru süresi çok doldu. Lütfen daha ileri bir tarih seçin.",
  },
  too_far: {
    title: "Çok ileri bir tarih",
    description: "Bu tarih için rezervasyon henüz açılmadı.",
  },
  boat_class_unavailable: {
    title: "Tekne sınıfı uygun değil",
    description: "Seçilen tekne sınıfında şu anda uygun tekne bulunmuyor.",
  },
  guest_class_restricted: {
    title: "Misafir rezervasyonu kısıtı",
    description:
      "Hesabı olmayan misafirler yalnızca 4x sınıfına rezervasyon yapabilir. Üye girişi yaparak tüm sınıfları kullanabilirsiniz.",
  },
  consents_required: {
    title: "Beyanlar eksik",
    description: "Devam etmek için zorunlu beyanları onaylamanız gerekiyor.",
  },
  consent_required: {
    title: "Zorunlu beyan",
    description: "Zorunlu beyanlar reddedilemez.",
  },
  unknown_consent: { title: "Geçersiz beyan", description: "Bilinmeyen bir beyan gönderildi." },
  invalid_reminder: {
    title: "Geçersiz hatırlatma",
    description: "Hatırlatma süresi kulübün izin verdiği seçeneklerden biri olmalıdır.",
  },
  otp_expired: {
    title: "Kod süresi doldu",
    description: "Doğrulama kodunun süresi doldu. Yeni kod isteyin.",
  },
  otp_invalid: { title: "Kod hatalı", description: "Girdiğiniz doğrulama kodu hatalı." },
  otp_not_found: {
    title: "Kod bulunamadı",
    description: "Aktif bir doğrulama kodu bulunamadı. Yeni kod isteyin.",
  },
  invalid_card_type: {
    title: "Geçersiz kart türü",
    description: "Üyelik türü Multisport veya Meditopia olmalıdır.",
  },
  card_number_required: {
    title: "Kart numarası gerekli",
    description: "Multisport kart numarası zorunludur ve yalnızca rakamlardan oluşmalıdır.",
  },
  code_not_found: {
    title: "Üye bulunamadı",
    description: "Bu koda sahip bir üye bulunamadı.",
  },
  cannot_friend_self: {
    title: "Geçersiz işlem",
    description: "Kendinizi arkadaş olarak ekleyemezsiniz.",
  },
  already_friends: { title: "Zaten arkadaşsınız", description: "Bu üyeyle zaten arkadaşsınız." },
  request_pending: {
    title: "İstek bekliyor",
    description: "Bu üyeyle bekleyen bir arkadaşlık isteği zaten var.",
  },
  not_friends: {
    title: "Arkadaşlık gerekli",
    description: "Bu işlemi yalnızca arkadaşlarınıza yapabilirsiniz.",
  },
  empty_message: { title: "Boş mesaj", description: "Boş mesaj gönderilemez." },
  message_too_long: {
    title: "Mesaj çok uzun",
    description: "Mesaj en fazla 2000 karakter olabilir.",
  },
  no_media: { title: "Medya yok", description: "Bu paylaşımda medya bulunmuyor." },
  media_invalid_type: {
    title: "Desteklenmeyen dosya",
    description: "Görsel JPG/PNG/GIF/WebP, video MP4/WebM/QuickTime olabilir.",
  },
  media_too_large: {
    title: "Dosya çok büyük",
    description: "Görseller en fazla 5MB, videolar en fazla 25MB olabilir.",
  },
  not_event: { title: "Etkinlik değil", description: "Bu paylaşım bir etkinlik değil." },
  invalid_package: {
    title: "Paket geçersiz",
    description: "Paket bulunamadı veya aktif değil.",
  },
  boat_taken: {
    title: "Tekne dolu",
    description: "Bu tekne aynı saatte başka bir seansa atanmış.",
  },
  instructor_busy: {
    title: "Eğitmen müsait değil",
    description: "Bu eğitmen aynı saatte başka bir seansta görevli.",
  },
  invalid_name: { title: "Geçersiz ad", description: "Ad alanı boş olamaz." },
  invalid_role: {
    title: "Geçersiz rol",
    description: "Rol CompanyAdmin veya Employee olmalıdır.",
  },
  user_not_found: { title: "Kullanıcı yok", description: "Kullanıcı bulunamadı." },
  caller_deactivated: {
    title: "Hesap devre dışı",
    description: "Hesabınız devre dışı bırakılmış.",
  },
  caller_not_found: { title: "Oturum hatası", description: "Oturum kullanıcısı bulunamadı." },
  cannot_demote_self: {
    title: "İşlem yapılamaz",
    description: "Kendi yönetici yetkinizi kaldıramazsınız.",
  },
  concurrency_conflict: {
    title: "Çakışma",
    description: "Kayıt başka bir kullanıcı tarafından güncellendi. Sayfayı yenileyip tekrar deneyin.",
  },
  unexpected_error: {
    title: "Beklenmeyen hata",
    description: "Beklenmeyen bir sorun oluştu. Lütfen tekrar deneyin.",
  },
};

/** Semantic copy for a code, or undefined when the server message should be used as-is. */
export function catalogLookup(code: string | undefined): ErrorCopy | undefined {
  if (!code) return undefined;
  return CATALOG[code];
}
