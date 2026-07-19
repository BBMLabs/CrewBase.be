# Yetkilendirme Matrisi

Spec bölüm 15 gereği, her endpoint için rol/policy/permission, tenant zorunluluğu, kaynak
sahipliği ve audit gereksinimini tek yerde tutar. Bir endpoint eklenirken veya yetkisi
değiştirilirken hem bu dosya hem `docs/API_ENDPOINTS.md` güncellenmelidir.

Şu an yalnızca Identity modülü implemente edildiği için matris bu modülle sınırlıdır. Diğer
modüller (Clubs, Memberships, Scheduling, Packages, Notifications, Reporting) implemente
edildikçe spec bölüm 12'deki tam rol seti (`Member`, `Instructor`, `ClubOfficer`, `ClubAdmin`,
`PlatformAdmin`, `SystemService`) ve yetki matrisi bu dosyaya eklenecektir - Identity, kulüp
üyeliği kavramını bilmediği için (platform seviyesinde bir bounded context) o roller bu modülün
endpointlerine uygulanmaz.

| Endpoint | Roller | Policy | Permission | Tenant Gerekli | Kaynak Sahipliği | Platform Admin Bypass | Audit |
|---|---|---|---|---|---|---|---|
| `POST /api/v1/auth/register` | Herkes (anonim) | Yok | Yok | Hayır | Yok | Yok | Hayır ⚠️ |
| `POST /api/v1/auth/login` | Herkes (anonim) | Yok | Yok | Hayır | Yok | Yok | Hayır ⚠️ |
| `POST /api/v1/auth/refresh` | Herkes (anonim, geçerli refresh token sahibi) | Yok | Yok | Hayır | Sunulan token'ın hash'i eşleşmeli | Yok | Hayır ⚠️ |
| `POST /api/v1/auth/logout` | Herkes (anonim, geçerli refresh token sahibi) | Yok | Yok | Hayır | Sunulan token'ın hash'i eşleşmeli | Yok | Hayır ⚠️ |
| `POST /api/v1/auth/logout-all` | Kimliği doğrulanmış herhangi bir kullanıcı | `RequireAuthorization()` (varsayılan) | Yok | Hayır | Yalnızca `ICurrentUser.UserId`'ye ait oturumlar - body/route'tan kullanıcı kimliği asla alınmaz | Yok | Hayır ⚠️ |
| `GET /health` | Herkes (anonim) | Yok | Yok | Hayır | Yok | Yok | Hayır |
| `GET /metrics` | Herkes (anonim - network seviyesinde kısıtlanmalı) | Yok | Yok | Hayır | Yok | Yok | Hayır |

⚠️ = bkz. `docs/API_ENDPOINTS.md` "Bilinen Riskler" - DB tabanlı audit log bu modülde henüz yok.

## Tenant İzolasyonu Notu

Identity modülünün hiçbir endpoint'i `X-Club-Id` header'ı beklemez veya kullanmaz - `User` aggregate'i
platform seviyesindedir, `ITenantOwned` uygulamaz (spec bölüm 2, kural 1-2: bir kullanıcı birden
fazla kulübe farklı rollerle üye olabilir, bu ilişki Memberships modülünde tutulacaktır).
`HttpContextCurrentTenant` (`src/RowingClub.Api/Security/HttpContextCurrentTenant.cs`) altyapısı
zaten mevcuttur ve `X-Club-Id` header'ını okur, ancak header'ı kullanıcının gerçek aktif
üyelikleriyle doğrulama adımı (spec bölüm 7, kural 2) **Memberships modülü** implemente
edildiğinde eklenecektir - o modül olmadan doğrulanacak bir üyelik verisi yoktur. Bu, mevcut
kodda bilinçli olarak eksik bırakılmış tek güvenlik kontrolüdür ve Memberships modülünün
Definition of Done'ının bir parçası olmalıdır.

## Refresh Token Reuse - Otomatik Güvenlik Tepkisi

`POST /api/v1/auth/refresh`, klasik rol/policy modelinin dışında bir güvenlik kontrolü içerir:
iptal edilmiş bir refresh token tekrar sunulursa (spec bölüm 11 - token reuse detection), bu
normal bir yetkilendirme reddi değil, bir **güvenlik olayı** olarak ele alınır - tüm token ailesi
ve ilişkili oturum sunucu tarafında proaktif olarak iptal edilir (kullanıcı hiçbir işlem
yapmasa bile). Bkz. `RefreshTokenCommandHandler` ve `RefreshToken.FlagReuse()`.
