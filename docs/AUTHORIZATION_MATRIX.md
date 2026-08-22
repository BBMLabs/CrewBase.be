# Yetkilendirme Matrisi

Her endpoint için rol/policy, tenant zorunluluğu ve kaynak sahipliği gereksinimini tek yerde
tutar. Bir endpoint eklenirken veya yetkisi değiştirilirken hem bu dosya hem
`docs/API_ENDPOINTS.md` güncellenmelidir.

## Roller

| Rol | Kimlik uzayı | Anlamı |
|---|---|---|
| `PlatformAdmin` | Firma kullanıcısı (`identity_users`) | Platformun tamamını yönetir; `PLATFORM_ADMIN_EMAIL` env'i ile açılışta seed edilir |
| `CompanyAdmin` | Firma kullanıcısı | Bir firmanın panelini tam yetkiyle yönetir |
| `Employee` | Firma kullanıcısı | Sınırlı erişim (şu an yalnızca placeholder `/admin/employee/tasks`) |
| `Member` | Üye (tenant DB `Customer`) | Kendi firmasının sitesinden kayıt olmuş üye; firma kullanıcılarından **ayrı kimlik uzayı** |

Bir kullanıcı aynı anda yalnızca bir role sahiptir (JWT `role` claim'i tekildir).

## Auth — `/api/v1/auth`

| Endpoint | Roller | Tenant Gerekli | Kaynak Sahipliği | Rate Limit |
|---|---|---|---|---|
| `POST /companies/register` | Anonim | Hayır | Yok | `auth` |
| `POST /login` | Anonim | Hayır | Yok | `auth` |
| `POST /refresh` | Anonim (geçerli refresh token) | Hayır | Sunulan token'ın hash'i eşleşmeli | `auth` |
| `POST /logout` | Anonim (geçerli refresh token) | Hayır | Sunulan token'ın hash'i eşleşmeli | `auth` |
| `POST /logout-all` | Herhangi bir authenticated kullanıcı | Hayır | Yalnızca `ICurrentUser.UserId`'ye ait oturumlar | `auth` |
| `POST /forgot-password` | Anonim | Hayır | Yok | `auth` |
| `POST /reset-password` | Anonim (geçerli reset token) | Hayır | Token sahibi | `auth` |
| `POST /verify-email` | Anonim (geçerli OTP kod) | Hayır | Token/kod sahibi | `auth` |
| `POST /send-verification-email` | Anonim | Hayır | Yok | `auth` |

## Platform Admin — `/api/v1/platform`

| Endpoint | Roller | Tenant Gerekli |
|---|---|---|
| `GET /companies` | `PlatformAdmin` | Hayır (platform seviyesi) |
| `GET /stats` | `PlatformAdmin` | Hayır |
| `GET /companies/pending` | `PlatformAdmin` | Hayır |
| `POST /companies/{companyId}/approve` | `PlatformAdmin` | Hayır |
| `POST /companies/{companyId}/suspend` | `PlatformAdmin` | Hayır |

## Admin (placeholder) — `/api/v1/admin`

| Endpoint | Roller | Policy | Kaynak Sahipliği |
|---|---|---|---|
| `GET /admin/{companyId}/dashboard` | `CompanyAdmin` | `SameCompany` | `{companyId}` == JWT `company_id` |
| `GET /admin/{companyId}/users` | `CompanyAdmin` | `SameCompany` | `{companyId}` == JWT `company_id` |
| `GET /admin/profile` | Herhangi bir authenticated kullanıcı | — | Yalnızca kendi profili |
| `GET /admin/employee/tasks` | `Employee` | — | Yalnızca kendi görevleri |

`SameCompanyAuthorizationHandler`, route'taki `{companyId}` ile JWT `company_id` claim'inin
birebir eşleştiğini doğrular; eşleşmezse `403`.

## Public Site — `/api/v1/public/{subdomain}` (Anonim)

| Endpoint | Roller | Tenant Gerekli | Notlar |
|---|---|---|---|
| `GET /info`, `/options`, `/consents`, `/availability` | Anonim | Evet — route'taki `{subdomain}` çözülür | Firma bulunamazsa `404 company_not_found` |
| `POST /appointments` | Anonim | Evet | Hesabı olmayan çağıran yalnızca `4x` alabilir (§ API_ENDPOINTS 4.1); beyanlar her seferinde zorunlu |

Bu grupta tenant, **route parametresinden** (subdomain) çözülür — JWT'den değil, çünkü çağıran
kimliksizdir. Subdomain string'i doğrudan bir DB adı olarak kullanılmaz; `TenantResolver`,
katalog DB'sindeki `Company.Subdomain` eşleşmesi üzerinden gerçek `DatabaseName`'i bulur.

## Üye (Member) — `/api/v1/member` ve `/api/v1/public/{subdomain}/members`

| Endpoint | Roller | Tenant Gerekli | Kaynak Sahipliği | Rate Limit |
|---|---|---|---|---|
| `POST /register`, `/login` | Anonim | Evet (subdomain'den) | Yok | `auth` |
| `GET/PUT/DELETE /me` | `Member` | Evet (JWT `company_id`'den) | Yalnızca `sub` == kayıt sahibi | — |
| `POST /otp/request`, `/otp/verify` | `Member` | Evet | Yalnızca kendi hesabı | — |
| `GET/POST /appointments`, `POST /appointments/{id}/cancel` | `Member` | Evet | Randevu, çağıranın `CustomerId`'sine ait olmalı (`403 forbidden` aksi halde) | — |
| `GET /packages`, `/consents`, `POST /consents`, `GET/PUT /cards` | `Member` | Evet | Yalnızca kendi verisi | — |
| `GET /code`, `/friends`, `POST /friends`, `/friends/{id}/accept\|reject` | `Member` | Evet | Arkadaşlık isteğini yalnızca **muhatap** yanıtlayabilir | — |
| `GET/POST /messages/{friendCustomerId}` | `Member` | Evet | Yalnızca **kabul edilmiş arkadaşlar** arasında (`403 not_friends` aksi halde) | — |
| `GET/POST /feed`, beğeni/yorum/katılım/takip uçları | `Member` | Evet | Paylaşım silme yalnızca kendi paylaşımı (`isMine`) | — |
| `wss /hubs/chat` | `Member` (`[Authorize(Roles="Member")]`) | Evet (bağlantı kimliği `{company_id}:{customerId}`) | Yalnızca kendi kanalına push alır | — |

Üye tarafında tenant, oturumdaki `company_id` claim'inden (`ResolveByCompanyIdAsync`) çözülür —
route'ta subdomain **yoktur**; bu, kayıtlı üyenin her istekte subdomain taşımasını gereksiz kılar.

## Firma Paneli — `/api/v1/company` (Roller: `CompanyAdmin`)

| Endpoint grubu | Roller | Tenant Gerekli | Kaynak Sahipliği |
|---|---|---|---|
| `/site`, `/stats` | `CompanyAdmin` | Evet | Yalnızca kendi firması |
| `/appointments*`, `/sessions*` | `CompanyAdmin` | Evet | Yalnızca kendi firmasının tenant DB'si |
| `/customers*`, `/package-balances`, `/logs*` | `CompanyAdmin` | Evet | Yalnızca kendi firmasının tenant DB'si; **derece değiştirme yalnızca firma admini** |
| `/instructors*`, `/boats*`, `/packages*` | `CompanyAdmin` | Evet | Yalnızca kendi firmasının tenant DB'si |
| `/settings*`, `/closed-dates*` | `CompanyAdmin` | Evet | Yalnızca kendi firmasının tenant DB'si |
| `/feed*` (moderasyon) | `CompanyAdmin` | Evet | Herhangi bir paylaşımı silebilir (moderasyon yetkisi) |
| `GET /users`, `POST /users`, `POST /users/{id}/role` | `CompanyAdmin` **+ handler seviyesinde yeniden doğrulama** | Evet | Hedef kullanıcı çağıranla **aynı firmada** olmalı; `PlatformAdmin` rolü verilemez; kendi admin yetkisini düşüremez |

### Firma kullanıcı yönetimi — çift katmanlı denetim

`POST /company/users` ve `POST /company/users/{id}/role`, endpoint seviyesindeki `CompanyAdmin`
rol denetiminin **üstüne**, handler içinde çağıranı DB'den yeniden yükleyip:
1. Hesabın aktif (`UserStatus.Active`) olduğunu,
2. Gerçekten `CompanyAdmin` olduğunu,
3. Hedef kullanıcının **aynı** `CompanyId`'ye ait olduğunu

doğrular (`CompanyUserGuards.EnsureCallerIsCompanyAdminAsync`). JWT'deki `role` claim'i tek
başına yeterli görülmez — bu, token içeriğiyle DB'deki güncel durum arasında (ör. hesap sonradan
deaktif edilmiş olabilir) fark olabileceği için bilinçli bir tasarım kararıdır.

## Tenant İzolasyonu — Genel Model

- **Katalog DB** (`rowingclub`): firmalar, firma kullanıcıları (`identity_users`), kimlik verileri.
- **Tenant DB** (`tenant_<subdomain>`, firma başına ayrı veritabanı): üyeler, randevular, seanslar,
  paketler, beyanlar, kartlar, arkadaşlıklar, mesajlar, akış paylaşımları.
- Her istek, işlenmeden önce `ITenantDatabase.Set(companyId, databaseName, subdomain)` ile
  doldurulmalıdır (`TenantResolver` bunu subdomain'den veya `company_id` claim'inden yapar).
  Doldurulmadan tenant repository'sine ulaşan bir akış **bilinçli olarak** istisna fırlatır — bu,
  yanlış firmanın veritabanına sessizce yazılmasını engelleyen son güvenlik katmanıdır.
- İstemciden gelen hiçbir `companyId`/tenant değerine güvenilmez; her zaman JWT veya route'taki
  doğrulanmış subdomain'den türetilir.

## Refresh Token Reuse — Otomatik Güvenlik Tepkisi

`POST /api/v1/auth/refresh`, klasik rol/policy modelinin dışında bir güvenlik kontrolü içerir:
iptal edilmiş bir refresh token tekrar sunulursa (token reuse detection), bu normal bir
yetkilendirme reddi değil, bir **güvenlik olayı** olarak ele alınır — tüm token ailesi ve
ilişkili oturum sunucu tarafında proaktif olarak iptal edilir. Bkz. `RefreshTokenCommandHandler`.

## Kişisel Veri Koruması

Tenant DB'deki tüm PII alanları (üye/eğitmen adı, telefon, e-posta, randevu notu, beyan IP'si,
kart numarası/fotoğrafı, mesaj içeriği, akış paylaşımı/medyası) AES-256-GCM ile şifreli saklanır;
aranabilir alanlar (telefon, e-posta) için HMAC blind index kullanılır — düz metin arama sütunu
yoktur. Şifre ve OTP kodları asla düz metin saklanmaz veya loglanmaz.
