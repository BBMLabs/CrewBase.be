# RowingClub API Dokümantasyonu — Frontend Geliştirici Rehberi

## 1. Genel Bilgiler

- **Base URL (dev):** `http://localhost:5000` (HTTP) / `https://localhost:7040` (HTTPS)
- **API Version:** `v1` (URL prefix ile belirtilir)
- **Global Response Formatı:**

```jsonc
// Başarılı (verili)
{
  "success": true,
  "data": { /* endpoint'e özel veri */ },
  "message": null,
  "code": null
}

// Başarılı (sadece mesaj)
{
  "success": true,
  "message": "İşlem başarılı.",
  "code": null
}

// Hata
{
  "success": false,
  "data": null,
  "message": "Hata açıklaması",
  "code": "ERROR_CODE"
}
```

- **Auth token (JWT):** `Authorization: Bearer <accessToken>` header'ı ile gönderilir.
- **Rate Limiting:** Auth endpoint'leri (login, register vs.) dakikada 10 istekle sınırlıdır. Çok fazla istek atarsanız `429 Too Many Requests` döner.

---

## 2. Auth Endpoints — `/api/v1/auth`

> **Not:** Tüm auth endpoint'leri rate limiter'a tabidir. 2FA endpoint'leri (`/2fa/*`) hariç diğerleri **authenticate gerektirmez** (kayıt, giriş, şifre sıfırlama vb. herkesin erişmesi gereken işlemlerdir).

---

### 2.1 Kayıt — `POST /api/v1/auth/register`

**Ne işe yarar?** Yeni bir kullanıcı hesabı oluşturur.

**Ne zaman kullanılır?** Kayıt formu doldurulup "Hesap Oluştur" butonuna basıldığında.

**İstek:**

```json
{
  "email": "ornek@email.com",
  "password": "GucluParola123!"
}
```

**Başarılı yanıt (201 Created):**

```json
{
  "success": true,
  "data": {
    "userId": "guid-1234",
    "email": "ornek@email.com",
    "createdAtUtc": "2026-07-25T12:00:00Z"
  }
}
```

**Olası hatalar:**
- `400 Bad Request` — email formatı geçersiz, parola çok zayıf
- `409 Conflict` — Bu email zaten kayıtlı

**Frontend notu:** Başarılı kayıttan sonra kullanıcıyı doğrudan login sayfasına yönlendirebilirsiniz. Email doğrulaması isteğe bağlı olarak `/send-verification-email` ile tetiklenebilir.

---

### 2.2 Şirket Kaydı — `POST /api/v1/auth/companies/register`

**Ne işe yarar?** Bir şirket ve onun admin kullanıcısını aynı anda oluşturur. Şirket kaydı **platform admin tarafından onaylanana kadar** bekler.

**Ne zaman kullanılır?** Şirket kaydı formu doldurulduğunda (hem şirket bilgileri hem admin bilgileri tek formda).

**İstek:**

```json
{
  "companyName": "Örnek Şirket A.Ş.",
  "adminEmail": "admin@sirket.com",
  "adminPassword": "GucluParola123!",
  "phone": "+905551234567",
  "contactEmail": "iletisim@sirket.com",
  "address": "İstanbul, Türkiye"
}
```

> `phone`, `contactEmail`, `address` alanları **isteğe bağlıdır** (null gönderilebilir).

**Başarılı yanıt (201 Created):**

```json
{
  "success": true,
  "data": {
    "companyId": "guid-company",
    "adminUserId": "guid-admin",
    "companyName": "Örnek Şirket A.Ş.",
    "adminEmail": "admin@sirket.com",
    "createdAtUtc": "2026-07-25T12:00:00Z"
  }
}
```

**Olası hatalar:**
- `400 Bad Request` — validasyon hatası (boş alan vs.)
- `409 Conflict` — Aynı şirket adı veya email zaten kayıtlı

**Frontend notu:** Kayıttan sonra kullanıcıya "Şirket kaydınız alındı, onay bekleniyor" mesajı gösterin. Şirket onaylanana kadar admin kullanıcı giriş yapabilir ancak şirket panosunu kullanamaz.

---

### 2.3 Giriş — `POST /api/v1/auth/login`

**Ne işe yarar?** Email + parola ile giriş yapar. Eğer kullanıcının 2FA'sı açıksa `requiresTwoFactor: true` döner ve ikinci adım gerekir.

**Ne zaman kullanılır?** Login formu gönderildiğinde.

**İstek:**

```json
{
  "email": "ornek@email.com",
  "password": "GucluParola123!"
}
```

**Başarılı yanıt (2FA KAPALI ise):**

```json
{
  "success": true,
  "data": {
    "requiresTwoFactor": false,
    "pendingTwoFactorToken": null,
    "accessToken": "eyJhbGciOiJSUzI1NiIs...",
    "accessTokenExpiresAtUtc": "2026-07-25T12:15:00Z",
    "refreshToken": "7c9e5f3b..."
  }
}
```

**Başarılı yanıt (2FA AÇIK ise):**

```json
{
  "success": true,
  "data": {
    "requiresTwoFactor": true,
    "pendingTwoFactorToken": "pending-token-string",
    "accessToken": null,
    "accessTokenExpiresAtUtc": null,
    "refreshToken": null
  }
}
```

**Olası hatalar:**
- `400 Bad Request` — validasyon
- `401 Unauthorized` — email veya parola yanlış

**Frontend notu (ÇOK ÖNEMLİ):**
- `requiresTwoFactor: true` gelirse, kullanıcıyı **2FA doğrulama ekranına** yönlendirin. `pendingTwoFactorToken`'ı geçici olarak frontend state'inde (veya sessionStorage'da) tutun.
- `requiresTwoFactor: false` gelirse, `accessToken`'ı localStorage/sessionStorage'a kaydedin ve ana sayfaya yönlendirin.

---

### 2.4 2FA ile Girişi Tamamlama — `POST /api/v1/auth/login/verify-2fa`

**Ne işe yarar?** 2FA kodu (veya kurtarma kodu) girilerek giriş işlemini tamamlar.

**Ne zaman kullanılır?** Login sonrası 2FA ekranında kod girildiğinde.

**İstek:**

```json
{
  "pendingToken": "login-API'sinden-gelen-pending-token",
  "code": "123456"
}
```

> `code` alanı: TOTP kodu (6 haneli) veya kurtarma kodu (10 karakterlik) olabilir.

**Başarılı yanıt:**

```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJSUzI1NiIs...",
    "accessTokenExpiresAtUtc": "2026-07-25T12:30:00Z",
    "refreshToken": "8d0f6e2a...",
    "remainingRecoveryCodes": 7
  }
}
```

> `remainingRecoveryCodes`: Eğer kurtarma kodu kullanıldıysa kalan kod sayısı. null ise TOTP kodu kullanılmıştır.

**Olası hatalar:**
- `400 Bad Request` — boş pendingToken veya code
- `401 Unauthorized` — hatalı/geçersiz/süresi dolmuş pendingToken veya yanlış kod

**Frontend notu:** Access token'ı kalıcı olarak kaydedin. `remainingRecoveryCodes` 5 veya altına düşerse kullanıcıya yeni kurtarma kodları oluşturmasını önerebilirsiniz.

---

### 2.5 Token Yenileme — `POST /api/v1/auth/refresh`

**Ne işe yarar?** Süresi dolan access token'ı yenilemek için kullanılır. Refresh token gönderilir, yeni bir access token + refresh token döner.

**Ne zaman kullanılır?** API'den `401 Unauthorized` alındığında (access token süresi doldu), otomatik olarak refresh yapılır.

**İstek:**

```json
{
  "refreshToken": "mevcut-refresh-token-string"
}
```

**Başarılı yanıt:**

```json
{
  "success": true,
  "data": {
    "accessToken": "yeni-erişim-tokeni",
    "accessTokenExpiresAtUtc": "2026-07-25T13:00:00Z",
    "refreshToken": "yeni-yenileme-tokeni"
  }
}
```

> **ÖNEMLİ:** Her refresh işleminde hem access token hem refresh token yenilenir. Eskileri geçersiz kalır.

**Olası hatalar:**
- `400 Bad Request` — boş refreshToken
- `401 Unauthorized` — geçersiz/süresi dolmuş/çalıntı refresh token

**Frontend notu (ÇOK ÖNEMLİ):**
- Bir **axios/fetch interceptor** yazın: 401 hatası alındığında otomatik olarak refresh endpoint'ini çağırsın, başarılı olursa eski token'ları yenileriyle değiştirip hata alan isteği tekrar denesin.
- Refresh token da başarısız olursa kullanıcıyı login sayfasına yönlendirin.

---

### 2.6 Oturum Kapatma — `POST /api/v1/auth/logout`

**Ne işe yarar?** Sadece gönderilen refresh token'ı geçersiz kılar. **Authorization header gerekmez.**

**Ne zaman kullanılır?** "Çıkış Yap" butonuna basıldığında (tek cihazdan çıkış).

**İstek:**

```json
{
  "refreshToken": "geçersiz-kılınacak-refresh-token"
}
```

**Başarılı yanıt:**

```json
{
  "success": true,
  "message": "Oturum kapatıldı.",
  "code": null
}
```

**Frontend notu:** Refresh token'ı localStorage'dan silin ve kullanıcıyı login sayfasına yönlendirin.

---

### 2.7 Tüm Cihazlardan Çıkış — `POST /api/v1/auth/logout-all`

**Ne işe yarar?** Kullanıcının **tüm** refresh token'larını geçersiz kılar. **Authorization gerekir.**

**Ne zaman kullanılır?** "Tüm cihazlardan çıkış yap" butonu veya şifre değiştirme sonrası güvenlik önlemi olarak.

**İstek:** Body yok. Sadece `Authorization: Bearer <accessToken>` header'ı yeterli.

**Başarılı yanıt:**

```json
{
  "success": true,
  "message": "Tüm oturumlar kapatıldı.",
  "code": null
}
```

**Olası hatalar:**
- `401 Unauthorized` — geçersiz veya eksik token

**Frontend notu:** Bu endpoint çağrıldığında refresh token'ın bir önemi kalmaz çünkü tüm token'lar backend'de geçersiz kılınır.

---

### 2.8 Şifre Sıfırlama — `POST /api/v1/auth/forgot-password`

**Ne işe yarar?** Kullanıcının email adresine şifre sıfırlama bağlantısı (token içeren) gönderir.

**Ne zaman kullanılır?** "Şifremi Unuttum" formu gönderildiğinde.

**İstek:**

```json
{
  "email": "ornek@email.com"
}
```

**Başarılı yanıt:**

```json
{
  "success": true,
  "message": "Parola sıfırlama bağlantısı e-posta adresinize gönderildi."
}
```

> **Güvenlik notu:** Email sistemde kayıtlı olsa da olmasa da aynı mesaj döner (email var mı bilgisi sızdırılmaz).

**Frontend notu:** Her zaman başarılı mesaj gösterin. Kullanıcıya email kutusunu kontrol etmesini söyleyin.

---

### 2.9 Şifre Sıfırlama (Onay) — `POST /api/v1/auth/reset-password`

**Ne işe yarar?** Email'den gelen token ile yeni şifre belirlenir.

**Ne zaman kullanılır?** Kullanıcı email'deki linke tıklayıp yeni şifre girdiğinde.

**İstek:**

```json
{
  "email": "ornek@email.com",
  "token": "emailden-gelen-token",
  "newPassword": "YeniGucluParola456!"
}
```

**Başarılı yanıt:**

```json
{
  "success": true,
  "message": "Parolanız başarıyla sıfırlandı."
}
```

**Olası hatalar:**
- `400 Bad Request` — validasyon (boş alan, zayıf şifre)
- `409 Conflict` — geçersiz/süresi dolmuş token

**Frontend notu:** Başarılı sıfırlama sonrası kullanıcıyı login sayfasına yönlendirin. Token'ı URL'den (query parameter) alın.

---

### 2.10 Email Doğrulama — `POST /api/v1/auth/verify-email`

**Ne işe yarar?** Email adresini bir token ile doğrular.

**Ne zaman kullanılır?** Kullanıcı email'deki doğrulama linkine tıkladığında.

**İstek:**

```json
{
  "email": "ornek@email.com",
  "token": "emailden-gelen-dogrulama-tokeni"
}
```

**Başarılı yanıt:**

```json
{
  "success": true,
  "message": "E-posta adresiniz başarıyla doğrulandı."
}
```

**Olası hatalar:**
- `400 Bad Request` — boş email veya token
- `409 Conflict` — geçersiz/süresi dolmuş token

**Frontend notu:** Token'ı URL'den alıp bu endpoint'e yönlendirin. Başarılı olursa "Email doğrulandı" mesajı gösterin.

---

### 2.11 Doğrulama Email'i Gönderme — `POST /api/v1/auth/send-verification-email`

**Ne işe yarar?** Email doğrulama bağlantısını (token'ı) tekrar gönderir.

**Ne zaman kullanılır?** Kullanıcı "Doğrulama email'ini tekrar gönder" butonuna bastığında.

**İstek:**

```json
{
  "email": "ornek@email.com"
}
```

**Başarılı yanıt:**

```json
{
  "success": true,
  "message": "Doğrulama e-postası gönderildi."
}
```

**Olası hatalar:**
- `400 Bad Request` — geçersiz email formatı

**Frontend notu:** Her zaman başarılı mesaj gösterin. Rate limit nedeniyle spam yapılamaz.

---

### 2.12 2FA Kurulum Bilgisi — `GET /api/v1/auth/2fa/setup`

**Ne işe yarar?** TOTP (Google Authenticator, Authy vb.) kurulumu için gerekli bilgileri döner: secret key ve QR kod (base64).

**Ne zaman kullanılır?** 2FA ayarları sayfası açıldığında, kullanıcı "2FA Kur" butonuna bastığında.

**İstek:** Body yok. `Authorization: Bearer <accessToken>` zorunlu.

**Başarılı yanıt:**

```json
{
  "success": true,
  "data": {
    "secret": "JBSWY3DPEHPK3PXP",
    "qrCodeBase64": "data:image/png;base64,iVBORw0KGgo..."
  }
}
```

> `qrCodeBase64` — doğrudan `<img src="...">` içinde kullanılabilecek base64 PNG verisi.

**Frontend notu:**
- Secret'ı kullanıcıya manuel giriş için gösterin (bir metin kutusunda).
- QR kodu `<img>` etiketi ile gösterin.
- Kullanıcı authenticator uygulamasına kaydettikten sonra `/2fa/enable` çağrılır.

---

### 2.13 2FA Etkinleştirme — `POST /api/v1/auth/2fa/enable`

**Ne işe yarar?** 2FA'yı kullanıcı için aktif eder.

**Ne zaman kullanılır?** Kullanıcı authenticator uygulamasına secret'ı kaydedip doğrulama kodunu girdiğinde.

**İstek:**

```json
{
  "method": "Totp",
  "code": "123456",
  "secret": "JBSWY3DPEHPK3PXP"
}
```

> `method` şu an için sadece `"Totp"` desteklenir. `secret`, `/2fa/setup`'tan gelen secret ile aynı olmalıdır.

**Başarılı yanıt:**

```json
{
  "success": true,
  "message": "İki faktörlü doğrulama etkinleştirildi."
}
```

**Olası hatalar:**
- `409 Conflict` — hatalı kod veya secret uyuşmazlığı

**Frontend notu:** Başarılı etkinleştirme sonrası kullanıcıya kurtarma kodlarını gösterin (`/2fa/recovery-codes`'u çağırarak). Kurtarma kodlarını bir kereye mahsus ekranda gösterip kaydetmelerini isteyin.

---

### 2.14 2FA Devre Dışı Bırakma — `POST /api/v1/auth/2fa/disable`

**Ne işe yarar?** 2FA'yı kapatır.

**Ne zaman kullanılır?** Kullanıcı 2FA ayarlarından "2FA'yı Kapat" dediğinde.

**İstek:**

```json
{
  "password": "mevcut-parola"
}
```

> Güvenlik için mevcut parola tekrar istenir.

**Başarılı yanıt:**

```json
{
  "success": true,
  "message": "İki faktörlü doğrulama devre dışı bırakıldı."
}
```

**Olası hatalar:**
- `409 Conflict` — yanlış parola

---

### 2.15 Kurtarma Kodları Oluşturma — `POST /api/v1/auth/2fa/recovery-codes`

**Ne işe yarar?** Yeni kurtarma kodları oluşturur (eskileri geçersiz kalır).

**Ne zaman kullanılır?**
- 2FA ilk etkinleştirildiğinde (kullanıcıya gösterilsin diye)
- Kullanıcı "Kurtarma kodlarını yenile" dediğinde

**İstek:** Body yok. `Authorization: Bearer <accessToken>` zorunlu.

**Başarılı yanıt:**

```json
{
  "success": true,
  "data": {
    "recoveryCodes": [
      "ABC123DEF4",
      "GHI567JKL8",
      "MNO901PQR2",
      "STU345VWX6",
      "YZA789BCD0",
      "EFG123HIJ4",
      "KLM567NOP8",
      "QRS901TUV2"
    ]
  }
}
```

> Her zaman **8 adet** kurtarma kodu döner.

**Frontend notu:** Bu kodları **sadece bir kere** ekranda gösterin ve kullanıcıyı bunları güvenli bir yere kaydetmesi için uyarın. Sayfa yenilenince bir daha gösterilmez (backend'de saklanmaz, hash'lenmiş halleri durur).

---

## 3. Platform Admin Endpoints — `/api/v1/platform`

> **Rol:** `PlatformAdmin` — Bu endpoint'ler sadece platform süper admin'leri tarafından kullanılabilir.

---

### 3.1 Bekleyen Şirketler — `GET /api/v1/platform/companies/pending`

**Ne işe yarar?** Henüz onaylanmamış (bekleyen) şirket kayıtlarını listeler.

**Ne zaman kullanılır?** Platform admin panelinde "Bekleyen Şirketler" sayfası açıldığında.

**Başarılı yanıt:**

```json
{
  "success": true,
  "data": [
    {
      "companyId": "guid-company-1",
      "name": "Örnek Şirket",
      "contactEmail": "iletisim@sirket.com",
      "phone": "+905551234567",
      "address": "İstanbul",
      "createdAtUtc": "2026-07-24T10:00:00Z"
    }
  ]
}
```

---

### 3.2 Şirket Onaylama — `POST /api/v1/platform/companies/{companyId}/approve`

**Ne işe yarar?** Bekleyen bir şirket kaydını onaylar. Şirket aktif hale gelir.

**Ne zaman kullanılır?** Platform admin "Onayla" butonuna bastığında.

**Başarılı yanıt:**

```json
{
  "success": true,
  "message": "Şirket onaylandı.",
  "code": null
}
```

---

### 3.3 Şirket Askıya Alma — `POST /api/v1/platform/companies/{companyId}/suspend`

**Ne işe yarar?** Aktif bir şirketi askıya alır. Şirket kullanıcıları giriş yapabilir ancak işlem yapamaz.

**Ne zaman kullanılır?** Platform admin "Askıya Al" butonuna bastığında.

**Başarılı yanıt:**

```json
{
  "success": true,
  "message": "Şirket askıya alındı.",
  "code": null
}
```

---

## 4. Admin / Şirket Endpoints — `/api/v1/admin`

### 4.1 Admin Panosu — `GET /api/v1/admin/{companyId}/dashboard`

**Ne işe yarar?** Şirket yöneticisi için pano bilgilerini döner (şu an placeholder).

**Roller:** `CompanyAdmin` + `SameCompany` policy (kullanıcı sadece kendi şirketine erişebilir).

**Ne zaman kullanılır?** Şirket yöneticisi ana sayfaya girdiğinde.

---

### 4.2 Çalışan Listesi — `GET /api/v1/admin/{companyId}/users`

**Ne işe yarar?** Şirketteki çalışanları listeler (şu an placeholder).

**Roller:** `CompanyAdmin` + `SameCompany` policy.

**Ne zaman kullanılır?** Şirket yöneticisi "Çalışanlar" sayfasını açtığında.

---

### 4.3 Profil — `GET /api/v1/admin/profile`

**Ne işe yarar?** Giriş yapmış kullanıcının profil bilgilerini döner.

**Rol:** Herhangi bir authenticated kullanıcı.

**Ne zaman kullanılır?** "Profilim" sayfası açıldığında.

---

### 4.4 Çalışan Görevleri — `GET /api/v1/admin/employee/tasks`

**Ne işe yarar?** Giriş yapmış çalışanın görev listesini döner (şu an placeholder).

**Rol:** `Employee`

**Ne zaman kullanılır?** Employee rolündeki kullanıcı "Görevlerim" sayfasını açtığında.

---

## 5. Auth Flow Diyagramı (Frontend İçin)

```
                  ┌──────────────────────┐
                  │   LOGIN FORM         │
                  │  (email + password)  │
                  └────────┬─────────────┘
                           │
                           ▼
                  ┌──────────────────────┐
                  │  POST /auth/login    │
                  └────────┬─────────────┘
                           │
                ┌──────────┴──────────┐
                ▼                     ▼
        ┌────────────────┐   ┌──────────────────┐
        │ 2FA KAPALI     │   │ 2FA AÇIK         │
        │ → accessToken  │   │ → pendingToken    │
        │ → refreshToken │   │ → 2FA ekranına   │
        │ → ANASAYFA     │   │   yönlendir      │
        └────────────────┘   └────────┬─────────┘
                                      │
                                      ▼
                             ┌─────────────────────┐
                             │ POST /auth/login/   │
                             │ verify-2fa          │
                             │ (code + pendingToken)│
                             └────────┬────────────┘
                                      │
                                      ▼
                             ┌─────────────────────┐
                             │ → accessToken        │
                             │ → refreshToken       │
                             │ → ANASAYFA           │
                             └─────────────────────┘
```

**Token Yönetimi (Interceptor):**

```
İstek → 401 hatası → POST /auth/refresh
                         ├─ Başarılı → yeni token'ları kaydet → eski isteği tekrar dene
                         └─ Başarısız → localStorage temizle → login sayfasına yönlendir
```

## 6. Önemli Notlar

| Konu | Açıklama |
|------|----------|
| **Token süresi** | Access token: 15 dk, Refresh token: 7 gün |
| **Rate limit** | Auth endpoint'leri: 10 istek / dakika |
| **Email işlemleri** | Şu an email gerçekten gitmez (development ortamı). Konsola log yazılır. |
| **Şirket kavramı** | Her kullanıcı bir şirkete bağlıdır. `SameCompany` policy'si kullanıcının sadece kendi şirket verisine erişmesini sağlar. |
| **2FA** | Sadece TOTP (Google Authenticator / Authy) desteklenir. SMS veya email 2FA yok. |

## 7. Response Status Code Özeti

| Status | Anlamı |
|--------|--------|
| `200 OK` | İşlem başarılı |
| `201 Created` | Kaynak başarıyla oluşturuldu |
| `400 Bad Request` | Validasyon hatası (eksik/hatalı alan) |
| `401 Unauthorized` | Token geçersiz/süresi dolmuş/eksik |
| `403 Forbidden` | Yetkiniz yok (rol veya şirket uyuşmazlığı) |
| `404 Not Found` | Kaynak bulunamadı |
| `409 Conflict` | İş mantığı hatası (çakışan email, geçersiz token, hatalı şifre vb.) |
| `429 Too Many Requests` | Rate limit aşıldı |
