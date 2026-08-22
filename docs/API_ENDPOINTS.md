# API Endpoint Dokümantasyonu

Bu dosya, geliştirme sürecinin merkezi endpoint dokümantasyonudur. Endpoint kodu değiştirilip bu
dosya güncellenmezse geliştirme tamamlanmış kabul edilmez.

> **Not:** Bu depo yalnızca bir **backend API**'dir; frontend `CrewBase.fe` reposundadır.
> Aşağıdaki tüm route'lar `RowingClub.Api` projesinde tanımlıdır
> (`src/RowingClub.Api/Endpoints/*.cs`).

## Global Sözleşmeler

- **Base URL (dev):** `http://localhost:5283` (launch profiline göre; `https://localhost:7040` de mevcuttur)
- **API versiyonu:** `v1`, URL prefix'i ile belirtilir (`/api/v1/...`); versiyonlama yalnızca
  `AuthEndpoints` route grubunda `Asp.Versioning` ile resmileştirilmiştir, diğer gruplar sabit
  `/api/v1/...` prefix'i kullanır.
- **Zarf (envelope):** Tüm JSON yanıtları `ApiResponse<T>` / `ApiResponse` ile sarmalanır:

  ```jsonc
  // Veri döndüren başarılı yanıt
  { "success": true, "data": { /* ... */ }, "message": null, "code": null }

  // Yalnızca mesaj döndüren başarılı yanıt
  { "success": true, "data": null, "message": "İşlem tamamlandı.", "code": null }

  // Hata (endpoint'in erken döndüğü, doğrulama/bulunamadı gibi durumlar)
  { "success": false, "data": null, "message": "Açıklama", "code": "error_code" }
  ```

- **Domain/iş kuralı hataları** (`DomainException`, `NotFoundException`,
  `AuthenticationFailedException`, FluentValidation hataları) `GlobalExceptionHandler` tarafından
  yakalanıp **RFC 7807 ProblemDetails** olarak döner (zarf değil):

  ```json
  {
    "type": "https://errors.rowingclub.dev/slot_full",
    "title": "Conflict",
    "status": 409,
    "detail": "Bu saatte 2x teknelerinin tümü dolu. Lütfen başka bir saat seçin.",
    "instance": "/api/v1/public/demo/appointments",
    "traceId": "...",
    "code": "slot_full"
  }
  ```

  Doğrulama hataları `400` + `code: "validation_error"`, beklenmeyen hatalar `500` +
  `code: "unexpected_error"` döner.
- **Kimlik doğrulama:** `Authorization: Bearer <accessToken>` (RSA imzalı JWT). Üç ayrı JWT
  "kimlik uzayı" vardır — `sub` claim'i farklı bir şeyi ifade eder:
  - **Platform / Firma kullanıcıları** (`PlatformAdmin`, `CompanyAdmin`, `Employee`): `sub` =
    `identity_users.Id`, `/api/v1/auth/login` ile alınır.
  - **Üyeler** (`Member` rolü): `sub` = tenant DB'deki `Customer.Id`, `company_id` claim'i
    zorunludur, `/api/v1/public/{subdomain}/members/{register|login}` ile alınır. Bu iki kimlik
    uzayı birbirine karışmaz (aynı `sub` değeri farklı tenant'larda farklı kişilere ait olabilir).
- **Rate limiting:** `auth` politikası (IP başına dakikada `AUTH_RATE_LIMIT` istek, varsayılan 10)
  `/api/v1/auth/*` ve `/api/v1/public/{subdomain}/members/*` route gruplarına uygulanır. Aşımda
  `429 Too Many Requests`.
- **Tenant çözümü:** Public/Member uçları subdomain'den (`TenantResolver.ResolveBySubdomainAsync`),
  Company panel uçları oturumdaki `company_id` claim'inden (`ResolveByCompanyIdAsync`) firmayı
  bulur ve `ITenantDatabase`'i doldurur; bulunamazsa `404 company_not_found`. İstemciden gelen
  hiçbir `companyId`/tenant değeri güvenilir kabul edilmez.
- **CORS:** `*.localhost` ve `*.faturebase.com` origin'lerine `AllowCredentials` ile açıktır
  (SignalR negotiate isteği credentials gerektirir).

---

## 1. Auth — `/api/v1/auth` (rate-limited)

Platform ve firma kullanıcılarının kimlik akışı. Endpoint kaynağı: `AuthEndpoints.cs`.

| Metot | Route | Auth | Açıklama |
|---|---|---|---|
| POST | `/companies/register` | Anonim | Firma + admin kullanıcısını **tek adımda ve doğrudan aktif** olarak oluşturur (onay beklemez) |
| POST | `/login` | Anonim | E-posta/parola ile giriş, access+refresh token çifti |
| POST | `/refresh` | Anonim (refresh token sahibi) | Access token'ı refresh token rotate ederek yeniler |
| POST | `/logout` | Anonim (refresh token sahibi) | Sunulan refresh token'ın ailesini/oturumunu iptal eder (idempotent, her zaman `200`) |
| POST | `/logout-all` | Bearer JWT | Kullanıcının **tüm** oturumlarını iptal eder |
| POST | `/forgot-password` | Anonim | Şifre sıfırlama linki e-postayla gönderilir (var/yok bilgisi sızdırılmaz) |
| POST | `/reset-password` | Anonim | Token ile yeni şifre belirler |
| POST | `/verify-email` | Anonim | **6 haneli OTP kodu** ile e-postayı doğrular |
| POST | `/send-verification-email` | Anonim | E-posta doğrulama OTP kodunu (yeniden) gönderir |

> **2FA endpoint'leri bilinçli olarak pasife alınmıştır** (`/login/verify-2fa`, `/2fa/setup`,
> `/2fa/enable`, `/2fa/disable`, `/2fa/recovery-codes` — hiçbiri map edilmiyor, `404` döner).
> Application katmanındaki handler'lar hâlâ mevcuttur; yeniden açmak `AuthEndpoints.cs`'e route
> map'lerini geri eklemekten ibarettir.

### 1.1 Firma Kaydı — `POST /companies/register`

**Request:**
```json
{
  "companyName": "Örnek Kürek Kulübü",
  "adminEmail": "admin@sirket.com",
  "taxNumber": "1234567890",
  "phone": "+905551234567",
  "contactEmail": "iletisim@sirket.com",
  "address": "İstanbul"
}
```
`taxNumber` 10 haneli vergi kimlik no veya 11 haneli T.C. kimlik no olmalıdır. `phone`,
`contactEmail`, `address`, `taxNumber` **zorunludur** — istekte **parola alanı yoktur**.

**Başarılı yanıt `201 Created`:**
```json
{
  "companyId": "guid",
  "adminUserId": "guid",
  "companyName": "Örnek Kürek Kulübü",
  "adminEmail": "admin@sirket.com",
  "subdomain": "ornek-kurek-kulubu",
  "siteUrl": "https://ornek-kurek-kulubu.faturebase.com",
  "createdAtUtc": "2026-01-01T10:00:00Z"
}
```

**İş kuralları:**
- Şirket adından benzersiz bir `subdomain` üretilir (çakışmada `-2`, `-3`... eklenir).
- Bu isimle **ayrı bir PostgreSQL veritabanı** (`tenant_<subdomain>`) provision edilir ve EF
  migration'ları uygulanır (`ITenantDatabaseProvisioner`) — kayıt bu işlem bitene kadar tamamlanmaz.
- Firma **doğrudan `Active`** durumunda açılır; platform admin onayı gerekmez.
- **Kayıt parolasız tamamlanır**: yönetici hesabı, kullanıcının hiçbir zaman bilmediği rastgele bir
  parola hash'iyle açılır. Kayıt sonrası gönderilen hoş geldin e-postası, parolayı belirlemesi için
  `/reset-password` ile aynı `PasswordResetToken` mekanizmasını kullanan **48 saat geçerli** bir
  bağlantı içerir (`{PUBLIC_APP_URL}/parola-sifirla?token=&email=`). Bağlantı ulaşmaz/kaybolursa
  kullanıcı `/forgot-password` ile aynı akışı yeniden tetikleyebilir — ayrı bir "aktivasyon" ucu
  yoktur. Parola belirlenene kadar `/login` denemeleri doğal biçimde `401 unauthorized` döner.
- Hata: `409 company_name_taken`, `409 email_already_registered`.

### 1.2 Giriş — `POST /login`

**Request:** `{ "email": "...", "password": "..." }`

**Başarılı yanıt `200 OK`:**
```json
{
  "requiresTwoFactor": false,
  "pendingTwoFactorToken": null,
  "accessToken": "eyJhbGciOi...",
  "accessTokenExpiresAtUtc": "2026-01-01T10:15:00Z",
  "refreshToken": "9f1c2e..."
}
```
`requiresTwoFactor` alanı DTO'da hâlâ mevcuttur ama handler artık her zaman `false` döner (2FA
akışı devre dışı — bkz. yukarı). JWT `sub`, `jti`, `email`, `iat` + `role` (`ClaimTypes.Role`) ve
firma kullanıcısıysa `company_id` claim'lerini taşır.

Hata: `401 unauthorized` (5 hatalı denemeden sonra hesap 15 dk kilitlenir).

### 1.3 Token Yenileme — `POST /refresh`

**Request:** `{ "refreshToken": "..." }` → **Response:** `accessToken`, `accessTokenExpiresAtUtc`,
`refreshToken` (Login ile aynı şekil).

**Reuse detection:** İptal edilmiş bir refresh token tekrar sunulursa bu bir çalıntı-token
sinyali sayılır: aynı `familyId`'ye sahip **tüm** token'lar ve ilişkili oturum iptal edilir,
`401` döner.

### 1.4–1.9 Diğer Auth uçları

| Endpoint | Request | Başarılı yanıt |
|---|---|---|
| `POST /logout` | `{ "refreshToken": "..." }` | `{ "message": "Oturum kapatıldı." }` — idempotent |
| `POST /logout-all` | (yok, Bearer zorunlu) | `{ "message": "Tüm oturumlar kapatıldı." }` |
| `POST /forgot-password` | `{ "email": "..." }` | `{ "message": "Parola sıfırlama bağlantısı e-posta adresinize gönderildi." }` — link `{PUBLIC_APP_URL}/parola-sifirla?token=&email=` şeklinde kurulur (`PUBLIC_APP_URL` env, varsayılan `https://faturebase.com`) |
| `POST /reset-password` | `{ "email", "token", "newPassword" }` | `{ "message": "Parolanız başarıyla sıfırlandı." }` |
| `POST /verify-email` | `{ "email", "token" }` — `token` = e-postaya giden **6 haneli kod** | `{ "message": "E-posta adresiniz başarıyla doğrulandı." }` |
| `POST /send-verification-email` | `{ "email": "..." }` | `{ "message": "Doğrulama e-postası gönderildi." }` |

`verify-email`/`send-verification-email` artık bağlantı değil **OTP kod** akışıdır: kod
`EmailVerificationToken` içinde hash'lenerek 15 dakika geçerli saklanır
(`SendVerificationEmailCommandHandler`).

---

## 2. Platform Admin — `/api/v1/platform` (Roles: `PlatformAdmin`)

Kaynak: `AdminEndpoints.cs`. Açılışta `PLATFORM_ADMIN_EMAIL`/`PLATFORM_ADMIN_PASSWORD` env
değişkenleri doluysa `PlatformAdminSeeder` bu rolde bir hesabı otomatik oluşturur.

| Metot | Route | Açıklama |
|---|---|---|
| GET | `/companies` | Tüm firmalar (her durumdan), en yeni önce |
| GET | `/stats` | `{ totalCompanies, activeCompanies, suspendedCompanies, registeredThisMonth }` |
| GET | `/companies/pending` | Geriye dönük uyumluluk için tutulan uç — firmalar artık otomatik aktif olduğundan pratikte boş liste döner |
| POST | `/companies/{companyId}/approve` | Firmayı aktifleştirir (askıya alınmışsa da geri açar) |
| POST | `/companies/{companyId}/suspend` | Firmayı askıya alır — site ve panel erişimi kapanır |

`PlatformCompanyDto`: `id, name, subdomain, status, phone, contactEmail, createdAtUtc`.

---

## 3. Admin (placeholder) — `/api/v1/admin`

Kaynak: `AdminEndpoints.cs`. Bu grup büyük ölçüde **placeholder**dır — gerçek firma paneli
işlevleri Bölüm 6'daki `/api/v1/company/*` uçlarındadır.

| Metot | Route | Roller/Policy | Açıklama |
|---|---|---|---|
| GET | `/admin/{companyId}/dashboard` | `CompanyAdmin` + `SameCompany` policy | Placeholder metin döner |
| GET | `/admin/{companyId}/users` | `CompanyAdmin` + `SameCompany` policy | Placeholder metin döner |
| GET | `/admin/profile` | Herhangi bir authenticated kullanıcı | `{ email, role }` özet metni |
| GET | `/admin/employee/tasks` | `Employee` | Placeholder metin döner |

`SameCompany` policy'si, route'taki `{companyId}` ile JWT'deki `company_id` claim'inin eşleştiğini
doğrular (`SameCompanyAuthorizationHandler`).

---

## 4. Public Site — `/api/v1/public/{subdomain}` (Anonim)

Kaynak: `PublicSiteEndpoints.cs`. Kayıtsız ziyaretçilerin firma sitesinden randevu almasını sağlar.

| Metot | Route | Açıklama |
|---|---|---|
| GET | `/` (kök) | API karşılama mesajı |
| GET | `/api/v1/public/{subdomain}/info` | Firma adı/telefon/e-posta/adres/site URL'i |
| GET | `/api/v1/public/{subdomain}/options` | Dinamik randevu kuralları: çalışma saatleri, tekne sınıfları+kapasiteleri, hatırlatma seçenekleri, aktif paketler, derece etiketleri |
| GET | `/api/v1/public/{subdomain}/consents` | Beyan kataloğu (bkz. §7.4) — üye kimliği olmadan, yalnızca metinler |
| GET | `/api/v1/public/{subdomain}/availability?date=&boatClass=&phone=` | Belirli gün+sınıf için slot listesi (`time, available, seatsLeft`) |
| POST | `/api/v1/public/{subdomain}/appointments` | Kayıtsız (misafir) randevu oluşturma |

### 4.1 Misafir Randevu — `POST /appointments`

**Request:**
```json
{
  "fullName": "Ayşe Yılmaz",
  "phone": "05551234567",
  "email": "ayse@example.com",
  "date": "2026-08-25",
  "time": "10:00",
  "boatClass": "4x",
  "note": "İlk dersim",
  "reminderMinutes": 60,
  "acceptedConsents": ["swim", "health", "rules", "kvkk"]
}
```

**İş kuralları (kritik):**
1. **Kimlik eşleştirme**: önce `phone`, sonra (bulunamazsa) `email` ile mevcut üye/misafir kaydı
   aranır — bir üye giriş yapmadan bu formu kullanırsa **kayıtlı derecesiyle** gruplanır.
2. **Misafir tekne kısıtı**: hesabı olmayan (üye olmayan) kişiler **yalnızca `4x`** rezervasyonu
   yapabilir; `1x`/`2x` denemesi `409 guest_class_restricted` ile reddedilir. Telefon/e-posta bir
   üyelikle eşleşirse tüm sınıflar açılır.
3. **Beyan zorunluluğu**: `Booking` kapsamındaki zorunlu beyanlar (yüzme, sağlık, kurallar, KVKK)
   her istekte kontrol edilir. **Misafir her randevuda yeniden onaylamalıdır**; kayıtlı üye
   (`HasAccount=true`) için önceki onaylar geçerli sayılır — eksikse `409 consents_required`.
4. Paket düşümü **kapatılmıştır** (`UsePackage: false` sabit) — yalnızca telefon numarasıyla
   başkasının paketi eritilemesin diye. Paketten düşerek randevu almak için üye girişi gerekir.
5. Aynı saatte aynı kişi için ikinci randevu: `409 already_booked`.
6. Slot/kısıt ihlalleri: `closed_date`, `too_soon`, `too_far`, `invalid_slot`, `slot_full`,
   `boat_class_unavailable`, `invalid_reminder`.

**Başarılı yanıt `201 Created`:**
```json
{
  "appointmentId": "guid", "sessionId": "guid",
  "date": "2026-08-25", "startTime": "10:00:00",
  "boatClass": "4x", "level": 0,
  "boatName": "Fırtına", "instructorName": "Koç Ahmet",
  "reminderMinutes": 60, "status": "Pending"
}
```

---

## 5. Üye (Member) — Auth + Self-servis

Kaynak: `MemberEndpoints.cs`. Üye kimliği JWT'de `sub = tenant DB'deki CustomerId`,
`role = "Member"`, `company_id = firma kimliği` olarak taşınır — bu, firma kullanıcılarının
kimlik uzayından tamamen ayrıdır.

### 5.1 Anonim: Kayıt/Giriş — `/api/v1/public/{subdomain}/members` (rate-limited)

| Metot | Route | Açıklama |
|---|---|---|
| POST | `/register` | Üyelik oluşturur; aynı telefonla misafir kaydı varsa hesap ona iliştirilir |
| POST | `/login` | E-posta/parola ile giriş |

**Register request:**
```json
{
  "fullName": "Deniz Ak",
  "phone": "05551234567",
  "email": "deniz@example.com",
  "password": "GucluParola123",
  "acceptedConsents": ["kvkk", "health-data"]
}
```
`Member` kapsamındaki zorunlu beyanlar (bkz. §7.4) eksikse `409 consents_required`. Hesabı zaten
olan telefon: `409 member_exists`. Kayıtlı e-posta: `409 email_taken`.

**Her iki uç da aynı şekli döner** (`200 OK`):
```json
{
  "member": { "customerId": "guid", "fullName": "...", "phone": "...", "email": "...",
              "level": 0, "levelLabel": "Hiç çekmedim", "memberCode": "KRK-7M2XQ4",
              "emailVerified": false, "phoneVerified": false,
              "defaultReminderMinutes": null, "createdAtUtc": "..." },
  "accessToken": "eyJ...",
  "expiresAtUtc": "2026-01-01T10:15:00Z"
}
```

### 5.2 Self-servis — `/api/v1/member` (Roles: `Member`)

Her istekte önce oturumdaki `company_id`'den firma çözülür (`404 company_not_found` olasılığı).

#### Profil ve hesap

| Metot | Route | Açıklama |
|---|---|---|
| GET | `/me` | Üye profilini döner |
| PUT | `/me` | `{ fullName, email, defaultReminderMinutes }` günceller |
| DELETE | `/me` | `{ password }` — doğru şifreyle **kalıcı (hard) silme**: üye, randevuları, paketleri, mesajları, logları geri dönüşsüz silinir. Yanlış şifre: `401` |

#### OTP doğrulama (e-posta / telefon)

| Metot | Route | Açıklama |
|---|---|---|
| POST | `/otp/request` | `{ "purpose": "email" \| "phone" }` — 6 haneli kod üretir, kayıtlı e-postaya gönderir (SMS sağlayıcısı bağlanana kadar telefon kodu da e-postayla gider) |
| POST | `/otp/verify` | `{ "purpose", "code" }` — doğrularsa `MemberDto` döner, `emailVerified`/`phoneVerified` `true` olur |

Kod 10 dakika geçerlidir, en fazla 5 yanlış deneme hakkı vardır (`otp_expired`, `otp_invalid`,
`otp_not_found`).

#### Randevular ve paketler

| Metot | Route | Açıklama |
|---|---|---|
| GET | `/appointments` | Üyenin randevu geçmişi + yaklaşanlar; her kayıtta aynı seanstaki diğer üyeler **maskeli isimle** görünür (`"Veli D."`) |
| POST | `/appointments` | Üye adına randevu; `usePackage: true` ile paketten düşülebilir |
| POST | `/appointments/{id}/cancel` | Yalnızca kendi randevusunu iptal edebilir (`403 forbidden` aksi halde); paketten alınmışsa ders otomatik iade edilir |
| GET | `/packages` | Üyeye tanımlı ders paketleri ve kalan ders sayıları |

`POST /appointments` request'i misafir formundakiyle aynıdır, `fullName`/`phone`/`email` yerine
profilden alınır; ek olarak `usePackage: boolean` taşır. `1x`/`2x`/`4x` kısıtı üyeler için
uygulanmaz (yalnızca hesabı olmayanlar `4x`'e sınırlıdır).

#### Beyanlar (consent)

| Metot | Route | Açıklama |
|---|---|---|
| GET | `/consents` | Katalog + üyenin güncel onay durumları (tarih + IP ile) |
| POST | `/consents` | `{ "entries": [{ "key": "photo", "accepted": true }] }` — zorunlu beyan reddedilemez (`consent_required`); isteğe bağlı rızalar (fotoğraf, ticari ileti) geri çekilebilir |

#### Üyelik kartları (Multisport / Meditopia)

| Metot | Route | Açıklama |
|---|---|---|
| GET | `/cards` | Üyenin kartları (fotoğraf dahil, base64) |
| PUT | `/cards` | Tür başına tek kart upsert eder |

**PUT /cards request:**
```json
{
  "type": "Multisport", "cardNumber": "1234567890", "companyName": "Acme A.Ş.",
  "expiryDate": "2027-01-01", "status": "Active",
  "photoBase64": "...", "photoContentType": "image/jpeg"
}
```
`type: "Multisport"` için `cardNumber` zorunlu (yalnızca rakam); `type: "Meditopia"` için
yalnızca `companyName` gerekir. Fotoğraf ≤5MB, JPG/PNG/GIF. Geçerlilik tarihi geçmişse durum
okuma anında otomatik `Expired` görünür.

#### Arkadaşlar ve gerçek zamanlı mesajlaşma

| Metot | Route | Açıklama |
|---|---|---|
| GET | `/code` | Üyenin **benzersiz kodu** (`KRK-XXXXXX`, yoksa ilk çağrıda üretilir) |
| GET | `/friends` | Arkadaş listesi (kabul edilmiş + gelen/giden bekleyen istekler), her biri için okunmamış mesaj sayısı |
| POST | `/friends` | `{ "memberCode": "KRK-..." }` ile arkadaşlık isteği gönderir |
| POST | `/friends/{id}/accept` | İsteği kabul eder (yalnızca muhatap yapabilir) |
| POST | `/friends/{id}/reject` | İsteği reddeder |
| GET | `/messages/{friendCustomerId}` | Aradaki mesaj geçmişi (yalnızca kabul edilmiş arkadaşlar arasında; okunanlar işaretlenir) |
| POST | `/messages/{friendCustomerId}` | `{ "body": "..." }` — mesaj gönderir, DB'ye şifreli yazar ve **SignalR ile anlık iletir** |

Mesajlaşma **SignalR** (`/hubs/chat`, `ChatHub`, `[Authorize(Roles = "Member")]`) üzerinden
gerçek zamanlıdır: istemci hub'a bağlanıp `"message"` olayını dinler, gönderim REST üzerinden
yapılır (kalıcılık + arkadaşlık doğrulaması orada). Kullanıcı kimliği `{company_id}:{customerId}`
olarak ayrıştırılır (`TenantUserIdProvider`), böylece farklı kulüplerdeki aynı ID'ler çakışmaz.
WebSocket bağlantıları `Authorization` header taşıyamadığından hub JWT'yi `?access_token=`
query parametresinden de kabul eder.

#### Kulüp akışı (feed)

| Metot | Route | Açıklama |
|---|---|---|
| GET | `/feed` | Son 50 paylaşım (kulüp + üyeler), beğeni/yorum/katılımcı sayıları ve "benim" durumlarımla |
| POST | `/feed` | Yazı/görsel/video paylaşır, isteğe bağlı etkinlik olarak işaretler |
| GET | `/feed/{id}/media` | Paylaşımın medyasını ayrı getirir (liste sorgusu medya taşımaz) |
| POST | `/feed/{id}/delete` | Yalnızca kendi paylaşımını silebilir |
| POST | `/feed/{id}/like` | Beğeniyi açar/kapatır, `{ active, count }` döner |
| GET | `/feed/{id}/comments` | Yorumları listeler |
| POST | `/feed/{id}/comments` | `{ "body": "..." }` yorum ekler |
| POST | `/feed/{id}/join` | Etkinliğe katılımı açar/kapatır |
| GET | `/feed/{id}/participants` | Etkinliğe katılan üyeler (ad + derece) |
| POST | `/follow/{customerId}` | Bir üyeyi takip et/bırak |

**POST /feed request:**
```json
{
  "body": "Pazar sabahı boğaz antrenmanı!",
  "mediaBase64": null, "mediaContentType": null,
  "isEvent": true, "eventTitle": "Boğaz Antrenmanı", "eventDate": "2026-08-30"
}
```
Medya: görsel (JPG/PNG/GIF/WebP) ≤5MB veya video (MP4/WebM/QuickTime) ≤25MB (≈15-20 sn); tür/boyut
sunucuda doğrulanır (`media_invalid_type`, `media_too_large`).

---

## 6. Firma Paneli — `/api/v1/company` (Roles: `CompanyAdmin`)

Kaynak: `CompanyPanelEndpoints.cs`. Her istek önce `ICurrentUser.CompanyId` ile firmayı çözer;
veriler yalnızca o firmanın **kendi tenant veritabanından** okunur/yazılır.

### 6.1 Genel

| Metot | Route | Açıklama |
|---|---|---|
| GET | `/site` | Firmanın site adresi ve mock yolu |
| GET | `/stats` | Panel ana sayfası istatistikleri (bugünkü randevu/seans, üye sayısı, bu ay randevu, önümüzdeki 7 gün, ay içi durum dağılımı, paket bakiyeleri) |

### 6.2 Randevular ve seanslar

| Metot | Route | Açıklama |
|---|---|---|
| GET | `/appointments?date=` | Randevu listesi (tarih verilmezse tümü) |
| POST | `/appointments/{id}/status` | `{ "status": "Pending"\|"Confirmed"\|"Cancelled"\|"Completed" }` — iptalde paket otomatik iade edilir, iptalden geri açmada tekrar düşülür |
| GET | `/sessions?date=` | O günün seansları: tekne/eğitmen ataması, kapasite, üye listesi |
| POST | `/sessions/{id}/assign` | `{ boatId?, instructorId? }` — otomatik atamayı elle ezer; çakışma varsa `boat_taken`/`instructor_busy` |

### 6.3 Üyeler

| Metot | Route | Açıklama |
|---|---|---|
| GET | `/customers` | Üye listesi |
| POST | `/customers` | `{ fullName, phone, email?, level }` ile panelden üye ekler |
| POST | `/customers/{id}/level` | **Yalnızca firma admini** üye derecesini (0-10) değiştirebilir |
| POST | `/customers/{id}/packages` | `{ lessonPackageId }` ile üyeye paket tanımlar |
| GET | `/customers/{id}/packages` | Üyenin paket bakiyeleri |
| GET | `/package-balances` | Tüm üyelerin paket bakiyeleri (düşüm takibi) |
| GET | `/customers/{id}/logs?take=` | Üye hareket geçmişi |
| GET | `/logs?take=` | Tüm üyelerin son hareketleri |

### 6.4 Kaynaklar (tekne / eğitmen / paket)

| Metot | Route | Açıklama |
|---|---|---|
| GET/POST | `/instructors` | Eğitmen listesi / oluşturma |
| PUT | `/instructors/{id}` | Eğitmen güncelleme (aktif/pasif dahil) |
| GET/POST | `/boats` | Tekne listesi / oluşturma (`boatClass`: `1x`\|`2x`\|`4x`) |
| PUT | `/boats/{id}` | Tekne güncelleme |
| GET/POST | `/packages` | Ders paketi listesi / oluşturma |
| PUT | `/packages/{id}` | Paket güncelleme (fiyat, ders sayısı, aktif/pasif) |

### 6.5 Ayarlar

| Metot | Route | Açıklama |
|---|---|---|
| GET | `/settings` | Çalışma saatleri, slot süresi, açık günler, min/max rezervasyon penceresi, hatırlatma seçenekleri, saat dilimi |
| PUT | `/settings` | Aynı alanları günceller — **tamamen firmaya özeldir**, tüm public/member akışları buradan beslenir |
| GET | `/closed-dates` | Kapalı (bayram/bakım) günler |
| POST | `/closed-dates` | `{ date, reason? }` ile tekil gün kapatır |
| DELETE | `/closed-dates/{date}` | Kapalı günü kaldırır |

**PUT /settings request:**
```json
{
  "openingTime": "08:00", "closingTime": "20:00", "slotMinutes": 60,
  "openDays": [1,2,3,4,5,6], "minNoticeHours": 1, "maxAdvanceDays": 14,
  "reminderOptions": [30,60,120,1440], "defaultReminderMinutes": 60,
  "timeZoneId": "Europe/Istanbul"
}
```
`openDays`: 0=Pazar ... 6=Cumartesi.

### 6.6 Kulüp akışı (moderasyon)

| Metot | Route | Açıklama |
|---|---|---|
| GET | `/feed` | Tüm akışı (üye görünümüyle aynı veri) görür |
| POST | `/feed` | **Kulüp adına** paylaşım yapar (`authorCustomerId: null`) |
| POST | `/feed/{id}/delete` | **Herhangi bir** paylaşımı kaldırabilir (moderasyon yetkisi) |
| GET | `/feed/{id}/media` \| `/comments` \| `/participants` | Salt okunur görüntüleme |

### 6.7 Panel kullanıcıları (firma içi yetkilendirme)

| Metot | Route | Açıklama |
|---|---|---|
| GET | `/users` | Firmanın panel kullanıcıları (`CompanyAdmin`/`Employee`) |
| POST | `/users` | `{ email, password, role }` ile yeni kullanıcı oluşturur |
| POST | `/users/{id}/role` | `{ role: "CompanyAdmin" \| "Employee" }` rol değiştirir |

**Güvenlik notu:** Bu üç uç `CompanyAdmin` rol denetiminin ÜSTÜNE, handler içinde çağıranı DB'den
yeniden yükleyip gerçekten `CompanyAdmin` ve aktif olduğunu doğrular
(`CompanyUserGuards.EnsureCallerIsCompanyAdminAsync`) — JWT'deki role claim'i tek başına
yeterli sayılmaz. `PlatformAdmin` rolü asla verilemez; hedef başka firmadaysa `forbidden`; admin
kendi yetkisini düşüremez (`cannot_demote_self`).

---

## 7. Ortak İş Kuralları

### 7.1 Kürek derecesi (0-10)

Derece **yalnızca firma admini** tarafından belirlenir (`POST /company/customers/{id}/level`);
üye kendi panelinde salt-okunur görür. Etiketler (`RowingLevels.Labels`): 0="Hiç çekmedim" ...
10="Milli takımda madalya kazandım". Randevu gruplaması aynı gün/saat/tekne sınıfı/**derece**
eşleşmesine göre yapılır.

### 7.2 Tekne kapasiteleri

`1x` = 1 kişi, `2x` = 2 kişi, `4x` = 4 kişi (`BoatClassExtensions.Capacity()`). Kapasite dolan bir
seansa yeni üye eklenemez; aynı slot+sınıf+derece için boş koltuklu seans varsa üye ona
gruplanır, yoksa boş tekne + müsait eğitmenle yeni seans açılır.

### 7.3 Hatırlatmalar

`SendDueRemindersCommand` her firma için dakikada bir taranır (`ReminderWorker`, arka plan
servisi); üyenin `defaultReminderMinutes` tercihi veya firma varsayılanı kullanılır, `0` =
hatırlatma istemiyorum.

### 7.4 Beyan kataloğu (`ConsentCatalog`)

| Key | Kapsam | Zorunlu | Başlık |
|---|---|---|---|
| `swim` | Booking | Evet | Yüzme Beyanı |
| `health` | Booking | Evet | Sağlık Beyanı |
| `rules` | Booking | Evet | Kürek Kulübü ve Rezervasyon Kuralları |
| `kvkk` | Booking | Evet | Kişisel Verilerin İşlenmesi Aydınlatma Metni |
| `health-data` | Member | Evet | Sağlık Verisi Açık Rıza |
| `photo` | Member | Hayır | Fotoğraf Açık Rıza (geri çekilebilir) |
| `marketing` | Member | Hayır | Ticari İleti (E-posta/SMS) (geri çekilebilir) |

`Booking` kapsamı **misafir her randevuda**, **kayıtlı üye bir kez** onaylar. Her onay tarih + IP
ile kaydedilir (IP tenant DB'de şifreli saklanır).

---

## 8. Sistem Uçları

| Route | Açıklama |
|---|---|
| `GET /health` | Liveness/health check (Postgres bağlantısı dahil) |
| `GET /metrics` | Prometheus scrape endpoint'i (`prometheus-net`) |
| `GET /openapi/v1.json` | OpenAPI şeması — yalnızca `Development` ortamında |
| `GET /scalar/v1` | Scalar API referans arayüzü — yalnızca `Development` ortamında |
| `wss /hubs/chat` | SignalR mesajlaşma hub'ı (bkz. §5.2) |

---

## Bilinen Riskler / Eksikler

- **Audit log**: `IAuditLogger` yalnızca yapısal loglama yapar (login, firma kullanıcı
  değişiklikleri); ayrı bir DB tablosu/koleksiyonu yoktur.
- **2FA**: Application katmanı hazır ama endpoint'ler kapalı (bkz. §1).
- **Employee rolü** için ayrı bir panel/uç seti henüz yoktur; `Employee` yalnızca placeholder
  `/admin/employee/tasks` ucuna erişebilir.
- **OTP/hatırlatma/hoş geldin e-postaları** `IEmailSender` (SMTP) üzerinden gider — gerçekten
  ulaşması ortamın SMTP ayarlarına bağlıdır; dev ortamda konsola da loglanabilir.
- **Idempotency-Key desteği** altyapısı (`IdempotencyBehavior`, Redis) hazır ama hiçbir command
  bunu şu an kullanmıyor.
