# API Endpoint Dokümantasyonu

Bu dosya, geliştirme sürecinin merkezi endpoint dokümantasyonudur (spec bölüm 15). Endpoint kodu
değiştirilip bu dosya güncellenmezse geliştirme tamamlanmış kabul edilmez.

Şu an yalnızca **Identity** modülü implemente edilmiştir. Diğer modüllerin endpointleri
(Clubs, Memberships, Scheduling, Packages, Notifications, Reporting) henüz yazılmamıştır - bkz.
`docs/ARCHITECTURE.md`.

---

## Register (Kullanıcı Kaydı)

### Genel Bilgi

| Alan | Değer |
|---|---|
| Modül | Identity |
| Metot | POST |
| Route | `/api/v1/auth/register` |
| API Versiyonu | v1 |
| Amaç | Yeni bir platform hesabı oluşturma |
| Authentication | Gerekmez |
| Roller | Herkes (anonim) |
| Policy | Yok |
| Permission | Yok |
| Tenant Gerekli | Hayır - Identity platform seviyesindedir, kulüpten bağımsızdır |
| Kaynak Sahipliği | Yok |
| Audit | Hayır (henüz DB tabanlı audit log altyapısı bu modülde yok - bkz. Bilinen Riskler) |
| Idempotency | Hayır (bu sürümde uygulanmadı) |
| Rate Limit | IP başına dakikada 10 istek (`auth` policy, tüm `/api/v1/auth/*` route grubu için) |

### Gerekli Header'lar

| Header | Zorunlu | Açıklama |
|---|---:|---|
| Content-Type | Evet | `application/json` |
| X-Correlation-Id | Hayır | İstek takip kimliği - verilmezse sunucu üretir, her yanıtta echo edilir |

### Request

```json
{
  "email": "uye@ornek.com",
  "password": "GucluBirParola123"
}
```

### Request Alanları

| Alan | Tip | Zorunlu | Validation |
|---|---|---:|---|
| email | string | Evet | `EmailAddress.Pattern` regex'ine uymalı, en fazla 254 karakter |
| password | string | Evet | En az 10, en fazla 128 karakter; en az bir harf ve bir rakam içermeli |

### Başarılı Response

HTTP `201 Created`

```json
{
  "userId": "00000000-0000-0000-0000-000000000000",
  "email": "uye@ornek.com",
  "createdAtUtc": "2026-01-01T10:00:00Z"
}
```

### Hata Response'ları

| Status | Code | Açıklama |
|---:|---|---|
| 400 | validation_error | E-posta formatı geçersiz veya parola kurallara uymuyor |
| 409 | email_already_registered | Bu e-posta adresi zaten kayıtlı |
| 429 | (Problem Details, rate limiter üretir) | Dakikalık istek limiti aşıldı |

### İş Kuralları

- E-posta normalize edilir (trim + lowercase) ve `identity_users.Email` üzerindeki unique
  index ile aynı adresin ikinci kez kaydına DB seviyesinde de izin verilmez.
- Parola asla düz metin saklanmaz - Argon2id ile hashlenir (spec bölüm 9).
- `UserRegisteredDomainEvent` aynı PostgreSQL transaction'ı içinde outbox'a yazılır.

### Uygulama Bağlantıları

- Command: `RegisterCommand`
- Validator: `RegisterCommandValidator`
- Handler: `RegisterCommandHandler`
- Unit test: `RegisterCommandHandlerTests`
- Security test: `AuthEndpointSecurityTests.Register_with_malformed_email_returns_400_not_500`,
  `Register_with_sql_injection_style_payload_is_rejected_by_validation_not_executed`

---

## Login (Giriş)

### Genel Bilgi

| Alan | Değer |
|---|---|
| Modül | Identity |
| Metot | POST |
| Route | `/api/v1/auth/login` |
| API Versiyonu | v1 |
| Amaç | E-posta/parola ile kimlik doğrulama, access+refresh token çifti alma |
| Authentication | Gerekmez |
| Roller | Herkes (anonim) |
| Tenant Gerekli | Hayır |
| Kaynak Sahipliği | Yok |
| Audit | Hayır (bkz. Bilinen Riskler) |
| Idempotency | Hayır |
| Rate Limit | IP başına dakikada 10 istek (brute-force koruması, spec bölüm 11) |

### Request

```json
{
  "email": "uye@ornek.com",
  "password": "GucluBirParola123"
}
```

### Request Alanları

| Alan | Tip | Zorunlu | Validation |
|---|---|---:|---|
| email | string | Evet | `EmailAddress.Pattern` regex'ine uymalı |
| password | string | Evet | Boş olamaz |

### Başarılı Response

HTTP `200 OK`

```json
{
  "accessToken": "eyJhbGciOi...",
  "accessTokenExpiresAtUtc": "2026-01-01T10:15:00Z",
  "refreshToken": "9f1c2e..."
}
```

### Hata Response'ları

| Status | Code | Açıklama |
|---:|---|---|
| 400 | validation_error | Request formatı geçersiz |
| 401 | unauthorized | E-posta veya parola hatalı, ya da hesap kilitli/deaktif (jenerik mesaj - hangisinin yanlış olduğu asla belirtilmez) |
| 429 | - | Dakikalık istek limiti aşıldı |

### İş Kuralları

- 5 başarısız denemeden sonra hesap 15 dakika kilitlenir (`IdentityOptions.MaxFailedLoginAttempts` /
  `LockoutDuration`).
- Başarılı girişte yeni bir refresh token **ailesi** (family) başlatılır ve bir `UserSession`
  kaydı oluşturulur (spec bölüm 11 - cihaz/oturum yönetimi).
- Access token `sub`, `jti`, `email`, `iat`, `iss`, `aud`, `exp` claim'lerini taşır - rol/tenant
  bilgisi taşımaz (spec bölüm 11).

### Uygulama Bağlantıları

- Command: `LoginCommand` / Handler: `LoginCommandHandler`
- Unit test: `LoginCommandHandlerTests`
- Security test: `AuthEndpointSecurityTests.Login_with_empty_credentials_returns_400_not_401`

---

## Refresh (Access Token Yenileme)

### Genel Bilgi

| Alan | Değer |
|---|---|
| Modül | Identity |
| Metot | POST |
| Route | `/api/v1/auth/refresh` |
| API Versiyonu | v1 |
| Amaç | Süresi dolan access token'ı, refresh token rotate ederek yenileme |
| Authentication | Gerekmez (refresh token body'de taşınır) |
| Tenant Gerekli | Hayır |
| Rate Limit | IP başına dakikada 10 istek |

### Request

```json
{
  "refreshToken": "9f1c2e..."
}
```

### Başarılı Response

HTTP `200 OK` - Login ile aynı şekil (`accessToken`, `accessTokenExpiresAtUtc`, `refreshToken`).

### Hata Response'ları

| Status | Code | Açıklama |
|---:|---|---|
| 400 | validation_error | `refreshToken` boş |
| 401 | unauthorized | Token bulunamadı, süresi dolmuş, oturum iptal edilmiş veya **yeniden kullanılan** (reuse) bir token |

### İş Kuralları (Token Family ve Reuse Detection - spec bölüm 11)

1. Sunulan token zaten **iptal edilmişse** (daha önce rotate edilmiş), bu bir çalıntı token
   sinyalidir: tüm aile (aynı `familyId`'ye sahip tüm token'lar) ve ilişkili oturum iptal edilir,
   `RefreshTokenReuseDetectedDomainEvent` outbox'a yazılır, `401` döner.
2. Token aktifse ama ilişkili `UserSession` iptal edilmişse (logout / logout-all sonrası) yine
   `401` döner - oturum, token satırından bağımsız olarak "aile"nin geçerliliğini belirler.
3. Aksi halde: eski token `Replaced` olarak işaretlenir, aynı `familyId` ile yeni bir token
   yayınlanır (rotation), oturumun `lastSeenAtUtc` alanı güncellenir.

### Uygulama Bağlantıları

- Command: `RefreshTokenCommand` / Handler: `RefreshTokenCommandHandler`
- Unit test: `RefreshTokenTests` (domain), entegrasyon senaryosu `RowingClub.IntegrationTests`

---

## Logout (Tek Cihazdan Çıkış)

### Genel Bilgi

| Alan | Değer |
|---|---|
| Modül | Identity |
| Metot | POST |
| Route | `/api/v1/auth/logout` |
| API Versiyonu | v1 |
| Amaç | Sunulan refresh token'ın ait olduğu oturumu/aileyi iptal etme |
| Authentication | Gerekmez (refresh token body'de taşınır) |
| Rate Limit | IP başına dakikada 10 istek |

### Request

```json
{ "refreshToken": "9f1c2e..." }
```

### Başarılı Response

HTTP `204 No Content`

### İş Kuralları

- **Idempotent tasarlanmıştır**: bilinmeyen/geçersiz bir token için de `204` döner - logout hiçbir
  zaman hata olarak değerlendirilmez.

### Uygulama Bağlantıları

- Command: `LogoutCommand` / Handler: `LogoutCommandHandler`

---

## Logout All (Tüm Cihazlardan Çıkış)

### Genel Bilgi

| Alan | Değer |
|---|---|
| Modül | Identity |
| Metot | POST |
| Route | `/api/v1/auth/logout-all` |
| API Versiyonu | v1 |
| Amaç | Kimliği doğrulanmış kullanıcının tüm aktif oturumlarını/refresh token ailelerini iptal etme |
| Authentication | **Bearer JWT zorunlu** |
| Roller | Herhangi bir kimliği doğrulanmış kullanıcı |
| Policy | `RequireAuthorization()` (varsayılan policy) |
| Kaynak Sahipliği | Kullanıcı yalnızca **kendi** oturumlarını iptal edebilir - `ICurrentUser.UserId` kullanılır, body'den kullanıcı kimliği alınmaz |
| Rate Limit | IP başına dakikada 10 istek |

### Gerekli Header'lar

| Header | Zorunlu | Açıklama |
|---|---:|---|
| Authorization | Evet | `Bearer <accessToken>` |

### Başarılı Response

HTTP `204 No Content`

### Hata Response'ları

| Status | Code | Açıklama |
|---:|---|---|
| 401 | unauthorized | Token yok, geçersiz veya süresi dolmuş |

### Uygulama Bağlantıları

- Command: `LogoutAllCommand` / Handler: `LogoutAllCommandHandler`
- Security test: `AuthEndpointSecurityTests.LogoutAll_without_a_token_returns_401`,
  `LogoutAll_with_a_garbage_bearer_token_returns_401_not_500`

---

## Health Check

| Alan | Değer |
|---|---|
| Route | `GET /health` |
| Authentication | Gerekmez |
| Amaç | Liveness probe (container orchestration) |

## Metrics

| Alan | Değer |
|---|---|
| Route | `GET /metrics` |
| Authentication | Gerekmez (production'da network seviyesinde kısıtlanmalı) |
| Amaç | Prometheus scrape endpoint'i (prometheus-net) |

---

## Bilinen Riskler / Eksikler

- **Audit log henüz yok**: bu 5 endpoint için DB tabanlı bir `AuditLog` tablosu/koleksiyonu
  implemente edilmedi. Kullanıcının global talimatı ("Audit Log: DB-based CRUD tracking... tüm
  kullanıcı işlemleri AuditLog tablosunda saklanmalı") bu modülün bir sonraki iterasyonunda ele
  alınmalıdır.
- **forgot-password / reset-password / verify-email** endpointleri (spec bölüm 14) bu sürümde
  implemente edilmedi. `EmailVerificationToken` ve `PasswordResetToken` domain modelleri ve Postgres
  tabloları hazır (bkz. `docs/DATA_MODEL.md`), ancak Application/Api katmanları yazılmadı -
  gönderilecek e-postaları işleyecek Notifications modülü de henüz scaffold aşamasındadır.
- **Idempotency-Key desteği** bu 5 endpoint için opsiyonel bırakıldı (hiçbiri `IIdempotentCommand`
  uygulamıyor) - altyapı (`IdempotencyBehavior`, Redis) hazır, ileride POST endpointlerine
  eklenmesi tek satırlık bir arayüz implementasyonu gerektirir.
