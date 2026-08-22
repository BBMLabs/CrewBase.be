# RowingClub API — İstemci Entegrasyon Rehberi

Bu depo **yalnızca bir backend API'dir**; frontend `CrewBase.fe` reposundadır. Bu doküman,
API'yi tüketecek herhangi bir istemci (mobil uygulama, üçüncü parti entegrasyon, iç test aracı)
için akış odaklı pratik bir rehberdir. Alan bazında eksiksiz referans için
`docs/API_ENDPOINTS.md`, rol/yetki tablosu için `docs/AUTHORIZATION_MATRIX.md`'ye bakın.

## 1. Genel Bilgiler

- **Base URL (dev):** `http://localhost:5283`
- **API sürümü:** `v1`, tüm route'lar `/api/v1/...` altındadır.
- **Yanıt zarfı:**

```jsonc
// Veri döndüren başarılı yanıt
{ "success": true, "data": { /* ... */ }, "message": null, "code": null }
// Yalnızca mesaj
{ "success": true, "data": null, "message": "İşlem tamamlandı.", "code": null }
// Endpoint-seviyesi hata (400/404 gibi erken dönüşler)
{ "success": false, "data": null, "message": "Açıklama", "code": "error_code" }
```

- **İş kuralı hataları** (domain exception'lar) zarf DEĞİL, RFC 7807 ProblemDetails döner:

```json
{ "type": "https://errors.rowingclub.dev/slot_full", "title": "Conflict", "status": 409,
  "detail": "Bu saatte 2x teknelerinin tümü dolu.", "code": "slot_full" }
```

İstemci tarafında her iki şekli de ele alan ortak bir hata ayrıştırıcı yazın: önce `code`/`detail`
(ProblemDetails), yoksa `code`/`message` (zarf) alanlarına bakın.

- **Kimlik doğrulama:** `Authorization: Bearer <accessToken>` (RSA imzalı JWT).
- **Rate limiting:** Auth ve üye kayıt/giriş uçları dakikada varsayılan 10 istekle sınırlıdır;
  aşımda `429`.
- **CORS:** `*.localhost` ve `*.faturebase.com` origin'lerine kimlik bilgileriyle (credentials)
  açıktır.

## 2. İki Ayrı Kimlik Uzayı — Önce Bunu Anlayın

Bu API'de **iki farklı JWT kimlik türü** vardır ve birbirine karıştırılmamalıdır:

| | Firma kullanıcıları | Üyeler |
|---|---|---|
| Roller | `PlatformAdmin`, `CompanyAdmin`, `Employee` | `Member` |
| `sub` claim'i neyi taşır | Katalog DB'deki `identity_users.Id` | Tenant DB'deki `Customer.Id` |
| Giriş ucu | `POST /api/v1/auth/login` | `POST /api/v1/public/{subdomain}/members/login` |
| Kayıt ucu | `POST /api/v1/auth/companies/register` (firma+admin birlikte) | `POST /api/v1/public/{subdomain}/members/register` |
| İş yaptığı DB | Katalog DB + kendi firmasının tenant DB'si (panel) | Yalnızca kendi firmasının tenant DB'si |

Bir üyenin token'ı ile `/api/v1/company/*`'ye, bir firma kullanıcısının token'ı ile
`/api/v1/member/*`'ye erişim `401`/`403` ile reddedilir.

## 3. Akış 1 — Firma Kaydı ve E-posta Doğrulama

```
POST /api/v1/auth/companies/register
  { companyName, adminEmail, adminPassword, phone?, contactEmail?, address? }
  ↓ 201 Created: { companyId, adminUserId, subdomain, siteUrl, ... }
  (arka planda: tenant veritabanı oluşturulur + hoş geldin e-postası gider)

POST /api/v1/auth/send-verification-email   { email }
  ↓ e-postaya 6 haneli OTP kodu gider (15 dk geçerli)

POST /api/v1/auth/verify-email   { email, token: "123456" }
  ↓ 200 OK
```

Firma **kayıt anında zaten aktiftir** — e-posta doğrulaması ve platform onayı beklemeden
`/api/v1/auth/login` ile giriş yapıp `/api/v1/company/*` panelini kullanabilir. Doğrulama isteğe
bağlı bir güven adımıdır, giriş engeli değildir.

## 4. Akış 2 — Firma Girişi ve Panel Kullanımı

```
POST /api/v1/auth/login   { email, password }
  ↓ 200 OK: { accessToken, accessTokenExpiresAtUtc, refreshToken }

GET /api/v1/company/stats        Authorization: Bearer <accessToken>
GET /api/v1/company/appointments
GET /api/v1/company/sessions?date=2026-08-25
...
```

`accessToken` süresi dolduğunda (`401` alındığında) `POST /api/v1/auth/refresh` ile
`{ refreshToken }` gönderip yeni bir çift alın; eski refresh token bu noktada geçersiz kalır
(rotation). Refresh de başarısızsa kullanıcıyı yeniden login'e yönlendirin.

**Önerilen istemci deseni:** bir fetch/axios interceptor'ı 401 yakalasın → refresh dener →
başarılıysa orijinal isteği yeni token'la tekrar dener → başarısızsa oturumu temizler.

## 5. Akış 3 — Misafir (Kayıtsız) Randevu Alma

Firma sitesi ziyaretçisi giriş yapmadan randevu alabilir, ama iki önemli kısıt vardır:

```
GET /api/v1/public/{subdomain}/options
  ↓ çalışma saatleri, tekne sınıfları+kapasiteleri, hatırlatma seçenekleri, paketler

GET /api/v1/public/{subdomain}/consents
  ↓ zorunlu beyan metinleri (yüzme, sağlık, kurallar, KVKK)

GET /api/v1/public/{subdomain}/availability?date=2026-08-25&boatClass=4x
  ↓ [{ time: "10:00", available: true, seatsLeft: 4 }, ...]

POST /api/v1/public/{subdomain}/appointments
  { fullName, phone, email?, date, time, boatClass: "4x", note?, reminderMinutes?,
    acceptedConsents: ["swim","health","rules","kvkk"] }
  ↓ 201 Created veya 409 (kısıt ihlali)
```

**Kısıt 1 — tekne sınıfı:** hesabı olmayan biri yalnızca `4x` alabilir. `1x`/`2x` denemesi
`409 guest_class_restricted` döner. Formunuzda misafir moddayken `boatClass` seçicisini `4x`'e
sabitleyin veya diğer seçenekleri devre dışı bırakıp açıklama gösterin.

**Kısıt 2 — kimlik eşleştirme:** girilen telefon **veya** e-posta backend'de mevcut bir üyelikle
eşleşirse (kayıt olmuş ama giriş yapmamış biri formu doldurduysa), rezervasyon otomatik olarak o
üyenin hesabına düşer ve **kayıtlı derecesine göre gruplanır** — tüm tekne sınıfları o kişi için
açılır. Bu durumda formda "Bu bilgilerle kayıtlı bir üyeliğiniz var, giriş yapmak ister misiniz?"
gibi bir mesaj göstermek isteyebilirsiniz (backend bunu ayrı bir sinyal olarak dönmez, sadece
işlemi başarıyla tamamlar).

**Beyanlar:** misafir formu **her seferinde** `acceptedConsents` göndermelidir; eksikse
`409 consents_required` + hangi beyanların gerektiği mesajda listelenir.

## 6. Akış 4 — Üye Kaydı, Giriş ve Randevu

```
POST /api/v1/public/{subdomain}/members/register
  { fullName, phone, email, password, acceptedConsents: ["kvkk","health-data"] }
  ↓ 200 OK: { member: {...}, accessToken, expiresAtUtc }

POST /api/v1/member/appointments   Authorization: Bearer <memberToken>
  { date, time, boatClass, note?, reminderMinutes?, usePackage: false,
    acceptedConsents: [] }   ← ilk randevudan sonra genelde boş kalabilir
```

Kayıtlı üye **randevu beyanlarını yalnızca ilk kez** onaylar; sonraki randevularda
`acceptedConsents: []` yeterlidir çünkü önceki onaylar geçerli sayılır. `GET /api/v1/member/consents`
ile hangi beyanların hâlâ eksik olduğunu önceden kontrol edip formda göstermek iyi bir pratiktir.

Üyeler tüm tekne sınıflarını kullanabilir ve `usePackage: true` ile tanımlı bir ders paketinden
düşerek rezervasyon yapabilir (`GET /api/v1/member/packages` ile kalan ders sayısını gösterin).

## 7. Akış 5 — Telefon/E-posta Doğrulama (OTP)

```
POST /api/v1/member/otp/request   { purpose: "phone" }
  ↓ kayıtlı e-postaya 6 haneli kod gider (SMS entegre olana kadar telefon kodu da e-postaya gider)

POST /api/v1/member/otp/verify   { purpose: "phone", code: "482913" }
  ↓ 200 OK: MemberDto (phoneVerified: true)
```

Kod 10 dakika geçerlidir, 5 yanlış denemeden sonra `otp_expired` ile kilitlenir — yeni kod
istenmesi gerekir. Aynı akış `purpose: "email"` ile e-posta doğrulaması için de kullanılır.

## 8. Akış 6 — Arkadaşlık ve Gerçek Zamanlı Mesajlaşma (SignalR)

```
GET /api/v1/member/code
  ↓ { memberCode: "KRK-7M2XQ4" }   ← paylaşılacak kod

POST /api/v1/member/friends   { memberCode: "KRK-9Q1ZAB" }
  ↓ istek gönderilir (Pending)

# karşı taraf:
POST /api/v1/member/friends/{friendshipId}/accept
  ↓ artık arkadaşlar, mesajlaşabilirler

GET /api/v1/member/messages/{friendCustomerId}   ← geçmiş mesajlar
POST /api/v1/member/messages/{friendCustomerId}  { body: "Selam!" }  ← REST ile gönder
```

**Gerçek zamanlı alım** SignalR ile yapılır:

```js
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5283/hubs/chat", { accessTokenFactory: () => memberToken })
  .withAutomaticReconnect()
  .build();

connection.on("message", (msg) => { /* msg: { id, senderId, recipientId, body, sentAtUtc } */ });
await connection.start();
```

Gönderme her zaman REST (`POST /messages/{id}`) ile yapılır — kalıcılık ve arkadaşlık doğrulaması
orada gerçekleşir; hub yalnızca alıcıya anlık push yapar. WebSocket bağlantısı `Authorization`
header'ı taşıyamadığı için token `accessTokenFactory` aracılığıyla query string'e eklenir; sunucu
tarafında bu yalnızca `/hubs/*` yoluna özel olarak kabul edilir (bkz. `AuthenticationSetup.cs`).

## 9. Akış 7 — Kulüp Akışı (Feed)

```
GET /api/v1/member/feed
  ↓ [{ id, authorName, isClubPost, body, mediaKind, isEvent, eventTitle, eventDate,
       likeCount, likedByMe, commentCount, participantCount, joinedByMe, isMine, ... }]

POST /api/v1/member/feed
  { body, mediaBase64?, mediaContentType?, isEvent, eventTitle?, eventDate? }

POST /api/v1/member/feed/{id}/like     ↓ { active: true, count: 4 }
POST /api/v1/member/feed/{id}/join     ↓ { active: true, count: 2 }   (yalnızca isEvent=true paylaşımlarda)
GET  /api/v1/member/feed/{id}/participants
```

Medya listede **taşınmaz** (performans için ayrı tabloda tutulur) — görsel/video göstermeden önce
`GET /feed/{id}/media` ile ayrıca çekin. Video yükleme öncesi istemci tarafında süre kontrolü
(≈15-20 sn) yapmanız önerilir; sunucu yalnızca dosya boyutunu (≤25MB) ve MIME tipini doğrular.

## 10. Önemli Notlar

| Konu | Açıklama |
|---|---|
| Token ömrü | Access token kısa ömürlü (dakikalar), refresh token günler mertebesinde — kesin süre `accessTokenExpiresAtUtc` alanından okunmalı, sabit değer varsayılmamalı |
| 2FA | Şu an **devre dışı** — ilgili endpoint'ler `404` döner |
| Şirket onayı | Yoktur — kayıt anında firma aktiftir |
| Tekne kapasiteleri | `1x`=1, `2x`=2, `4x`=4 kişi; `/options` yanıtından dinamik okuyun, sabit kodlamayın |
| Misafir kısıtı | Hesapsız kullanıcı yalnızca `4x` alabilir |
| Derece atama | Yalnızca firma paneli yapabilir; üye kendi derecesini değiştiremez |
| E-posta gönderimi | Dev ortamda gerçekten gitmeyebilir, SMTP ayarına bağlıdır |
| PII saklama | Tüm kişisel veri (ad, telefon, e-posta, mesaj, kart, beyan IP'si) sunucuda şifreli — istemci tarafında da bu verileri kalıcı depoya (localStorage vb.) yazmaktan kaçının, yalnızca oturum belleğinde tutun |

## 11. Response Status Code Özeti

| Status | Anlamı |
|---|---|
| `200 OK` | İşlem başarılı |
| `201 Created` | Kaynak oluşturuldu |
| `400 Bad Request` | Doğrulama hatası (eksik/hatalı alan) |
| `401 Unauthorized` | Token geçersiz/süresi dolmuş/eksik, ya da yanlış kimlik bilgisi |
| `403 Forbidden` | Doğru token ama bu kaynağa erişim yetkisi yok (rol veya sahiplik uyuşmazlığı) |
| `404 Not Found` | Kaynak veya firma (subdomain) bulunamadı |
| `409 Conflict` | İş kuralı ihlali (çakışan slot, eksik beyan, yanlış şifre vb.) |
| `429 Too Many Requests` | Rate limit aşıldı |
