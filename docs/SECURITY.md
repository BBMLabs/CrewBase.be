# Güvenlik

Bu doküman RowingClub backend'inin authentication, authorization, secret yönetimi, şifreleme, threat model ve audit yaklaşımını özetler. Detaylı endpoint bazlı yetki kuralları (henüz yazılmamış) `docs/AUTHORIZATION_MATRIX.md`'de tutulacaktır.

## 1. Authentication

### Access Token

- Kısa ömürlü JWT; `sub`, `jti`, `iss`, `aud`, `exp`, `iat` claim'lerini içerir.
- İmzalama RS256 veya ES256 ile yapılır; `kid` claim'i key rotation'ı destekler.
- Kritik yetkilendirme kararları **yalnızca token içindeki role güvenmez** — üyelik ve tenant erişimi her istekte sunucu tarafında yeniden doğrulanır.

### Refresh Token

- Rotation uygulanır: her refresh isteğinde eski token geçersiz kılınır, yeni bir token üretilir.
- **Reuse detection**: daha önce kullanılmış (rotate edilmiş) bir refresh token tekrar kullanılmaya çalışılırsa, o token'ın ait olduğu **token ailesi (family)** tamamen iptal edilir — bu, token'ın çalınmış olabileceğinin işaretidir.
- Refresh token veritabanında düz metin değil, hashlenmiş olarak saklanır.
- Tek cihazdan çıkış (`logout`) ve tüm cihazlardan çıkış (`logout-all`) desteklenir.
- Bu akışın tamamı **Identity modülünde implemente edilmektedir** (`/api/v1/auth/refresh`, `/api/v1/auth/logout`, `/api/v1/auth/logout-all`).

### Ek Kimlik Doğrulama Kontrolleri

- Rate limiting ve brute-force koruması (özellikle `login`/`refresh` endpoint'lerinde).
- Belirli sayıda başarısız girişten sonra hesap kilitleme.
- E-posta doğrulama zorunluluğu; opsiyonel MFA altyapısı.
- CORS allow-list (yalnızca bilinen origin'ler).
- Güvenli response header'ları (`Strict-Transport-Security`, `X-Content-Type-Options`, `X-Frame-Options`, `Content-Security-Policy` vb.).
- Swagger/OpenAPI production'da erişim kontrolüne tabidir (anonim açık değildir).
- Replay saldırısı koruması ve `Idempotency-Key` desteği (mutasyon endpoint'lerinde).

## 2. Authorization Modeli

Yalnızca `[Authorize(Roles=...)]` gibi basit role attribute kullanımı **yeterli görülmez**. Her istekte aşağıdaki kontroller birlikte uygulanır:

1. Kullanıcı doğrulaması (authentication).
2. Platform rolü.
3. Aktif tenant üyeliği.
4. Kulüp içi rol.
5. Permission (ince taneli izin).
6. Kaynak sahipliği (resource ownership).
7. İş kuralı (domain invariant).

### Roller

`Member`, `Instructor`, `ClubOfficer`, `ClubAdmin`, `PlatformAdmin`, `SystemService`.

### Policy Kavramı (örnekler)

`AuthenticatedUser`, `ActiveClubMember`, `InstructorOfActiveClub`, `ClubOfficerOrAbove`, `ClubAdminOnly`, `PlatformAdminOnly`, `CanManageMembers`, `CanManageSchedules`, `CanManagePackages`, `CanViewClubReports`, `CanManageClubSettings`, `CanManageOwnAppointment`, `CanRecordAttendance`, `SystemServiceOnly`.

Policy'ler ASP.NET Core authorization policy altyapısıyla tanımlanır ve her endpoint'e route group veya endpoint seviyesinde bağlanır. Handler seviyesinde ayrıca kaynak sahipliği (ör. "kullanıcı yalnızca kendi randevusunu iptal edebilir") ve tenant kontrolü uygulanır — bu ikisi endpoint policy'sinin yerini tutmaz, ona ek olarak çalışır.

Tam rol × işlem yetki matrisi bu dosyada tekrarlanmaz; kavramsal model burada anlatılır. Endpoint bazlı somut matris `docs/AUTHORIZATION_MATRIX.md`'de (Identity implementasyonu ilerledikçe eklenecek) tutulur.

**Varsayılan davranış: deny by default.** Belirsizlik durumunda erişim reddedilir, tenant filtresi olmadan hiçbir veri erişimine izin verilmez.

## 3. Secret Yönetimi

- Ortam dosyaları: `.env.developer`, `.env.product` — bu dosyalar Git'e eklenmez, `.gitignore` içinde tanımlıdır. Repository'de yalnızca `.env.example` tutulur.
- Production'da `.env.product` repository içinde saklanmaz; secret değerleri CI/CD secret store, Docker/Kubernetes secrets, Vault veya cloud secret manager üzerinden enjekte edilir.
- Uygulama, zorunlu bir secret eksikse **fail-fast** olur (başlatmayı reddeder), sessizce varsayılan değerle devam etmez.
- Secret değerleri hiçbir koşulda loglanmaz.
- Secret ve key rotation desteklenir (JWT imzalama anahtarları `kid` ile, alan şifreleme anahtarları `key version` ile).

`.env.example` içindeki değişken listesi için bkz. [DEVELOPMENT.md](./DEVELOPMENT.md#2-environment-değişkenleri).

## 4. Alan Bazlı Şifreleme (Field-Level Encryption)

Aşağıdaki alan kategorileri **AES-256-GCM** ile uygulama seviyesinde şifrelenir:

- Telefon
- Adres
- Acil durum kişisi
- Sağlık beyanları
- Kimlik veya belge numarası
- Özel üye notları
- Özel ders notları

Her şifreli değer şu bilgileri birlikte taşır:

- Cipher text
- Nonce
- Authentication tag
- Key version

Bu alanlar için plaintext arama yapılmaz. Arama gerekiyorsa, normalize edilmiş değer üzerinden ayrı bir **HMAC tabanlı blind index** üretilir (ör. telefon numarasına göre arama, plaintext telefon saklamadan HMAC(normalize(telefon), key) ile eşleştirilir).

Genel kurallar:

- TLS her ortamda zorunludur.
- Parolalar şifrelenerek değil, **hashlenerek** saklanır (Argon2id veya güvenli PBKDF2).
- Refresh token düz metin saklanmaz (hashlenir).
- Veritabanı yedekleri şifrelenir.

### MongoDB'ye Özgü Veri Deposu Güvenliği

Platformun tek veri deposu MongoDB olduğundan (bkz. [adr/0002-mongodb-tek-veritabani-stratejisi.md](./adr/0002-mongodb-tek-veritabani-stratejisi.md)), alan bazlı şifrelemeye ek olarak şu önlemler uygulanır:

- **Encryption at rest**: yönetilen (managed) bir MongoDB servisi kullanılıyorsa servisin yerleşik disk şifrelemesi; self-hosted bir replica set'te dosya sistemi/disk düzeyinde şifreleme (ör. LUKS) zorunludur. Bu, §4'teki alan bazlı şifrelemenin **yerine geçmez**, onu tamamlar — alan bazlı şifreleme uygulama kodundaki bir sızıntıya karşı da koruma sağlarken, encryption at rest yalnızca disk/yedek erişimine karşı korur.
- **Replica set kimlik doğrulaması**: production replica set'i keyfile veya x.509 tabanlı internal authentication ile çalışır; client bağlantı stringi kimlik bilgisi taşır ve diğer secret'lar gibi asla loglanmaz (bkz. §3).
- **Ağ izolasyonu**: MongoDB'ye erişim özel bir ağ/subnet (VPC, security group) ile sınırlandırılır; replica set node'ları public internet'e açık çalıştırılmaz, yalnızca `api` servisi ve yönetim araçları erişebilir.
- **Yetkilendirilmiş roller**: uygulamanın kullandığı MongoDB kullanıcısı yalnızca ilgili veritabanı üzerinde `readWrite` yetkisine sahiptir; cluster-admin yetkileri operasyonel/insan kullanıcılarına ayrılır.

Rasyonel için bkz. [adr/0003-tenant-izolasyon-stratejisi.md](./adr/0003-tenant-izolasyon-stratejisi.md) (tenant izolasyonu) ve ilgili şifreleme ADR'i eklendiğinde referans verilecektir.

## 5. Key Rotation

- JWT imzalama anahtarları `kid` claim'i ile rotate edilir; eski `kid` ile imzalanmış token'lar `exp` süresi dolana kadar doğrulanabilir kalır, yeni token'lar güncel anahtarla imzalanır.
- Alan şifreleme anahtarları `FIELD_ENCRYPTION_KEY_CURRENT` / `FIELD_ENCRYPTION_KEY_PREVIOUS` / `FIELD_ENCRYPTION_KEY_VERSION` üçlüsü ile yönetilir: yeni veriler her zaman `CURRENT` ile şifrelenir, `PREVIOUS` yalnızca eski verileri çözmek (decrypt) için tutulur; rotation tamamlandığında veriler kademeli olarak yeniden şifrelenir (re-encryption) ve `PREVIOUS` devre dışı bırakılır.
- Rotation sırasında hizmet kesintisi yaşanmaması için her iki anahtar sürümü de bir süre paralel desteklenir.

## 6. Threat Model (Temel)

| Tehdit | Karşı Önlem |
|---|---|
| Tenant ID manipülasyonu (başka kulübün verisine erişim) | Header'daki `X-Club-Id` sunucu tarafında üyelikle doğrulanır; route/header/context tutarlılığı kontrol edilir; `MongoUnitOfWork.Track()` her `ITenantOwned` aggregate'i izlemeye alırken `ICurrentTenant`'a karşı doğrular |
| IDOR (tahmin edilebilir ID ile başka kullanıcının kaynağına erişim) | Handler seviyesinde kaynak sahipliği kontrolü; GUID kullanımı; yetkisiz erişimde 403 veya güvenli 404 |
| Refresh token çalınması / replay | Rotation + reuse detection + token family iptali; hashlenmiş saklama |
| Brute-force / credential stuffing | Rate limiting, hesap kilitleme, gecikmeli/loglanan başarısız giriş denemeleri |
| Hassas veri sızıntısı (loglar, hata mesajları) | Log maskeleme, production'da stack trace döndürülmemesi, Problem Details standardı |
| Yetki yükseltme (privilege escalation) | Deny-by-default policy modeli, çok katmanlı authorization kontrolü (rol + tenant + permission + sahiplik + iş kuralı) |
| Injection (NoSQL) | MongoDB.Driver'ın tip güvenli, parametreli `Builders<T>.Filter` API'si (serbest string/BSON birleştirme yapılmaz), girdi validation |
| Man-in-the-middle | TLS zorunluluğu, güvenli header'lar (HSTS) |
| Idempotency ihlali (çift işlem) | `Idempotency-Key` header'ı ve idempotency pipeline behavior |
| Platform Admin yetkisinin kötüye kullanımı | Bypass işlemleri özel policy + zorunlu audit kaydı gerektirir |

## 7. OWASP Uyumlu Kontroller

Kontroller OWASP API Security Top 10 ve ASVS ile hizalıdır: broken object level authorization (IDOR karşı önlemleri), broken authentication (JWT + refresh rotation + rate limiting), excessive data exposure (DTO bazlı response, hassas alan filtreleme), lack of resources & rate limiting, broken function level authorization (policy + rol + permission katmanları), mass assignment (command/DTO ayrımı, whitelist alan bağlama), security misconfiguration (fail-fast secret kontrolü, production'da Swagger kısıtı), injection (parametreli sorgular), improper assets management (API versioning), insufficient logging & monitoring (bkz. [OBSERVABILITY.md](./OBSERVABILITY.md)).

## 8. Audit Logging

- Tüm önemli kullanıcı işlemleri (yaratma, güncelleme, silme, rol değişikliği, Platform Admin bypass'ları) audit edilir.
- Audit kayıtları DB tabanlı tutulur (yalnızca Serilog dosya logu yeterli değildir) — platformun tek veri deposu MongoDB'de, ayrı bir audit koleksiyonu ile saklanır (henüz implemente edilmedi; ilgili modülün geliştirmesiyle birlikte `docs/DATA_MODEL.md`'e eklenecektir).
- Audit kaydı en az şunları içerir: kim, ne zaman, hangi tenant'ta, hangi işlemi, hangi kaynak üzerinde, önceki/sonraki durum (mümkünse), correlation id.
- Audit gereksinimi her endpoint dokümantasyonunda (`API_ENDPOINTS.md`) ayrı bir alan olarak işaretlenir.

## 9. Rate Limiting

- ASP.NET Core rate limiting middleware kullanılır; kullanıcı, IP ve/veya endpoint bazlı limitler tanımlanabilir.
- Özellikle `auth` endpoint'leri (login, refresh, forgot-password) için daha sıkı limitler uygulanır.
- Rate limit aşımı `429` Problem Details response'u ile döner.
- Rate limit verileri (sayaçlar) Redis üzerinde dağıtık olarak tutulur (çoklu instance senaryosu için).

## 10. PII / Kişisel Veri Yönetimi

- Hassas kişisel veriler (§4'teki liste) alan bazlı şifrelenir; loglara asla tam değer yazılmaz, gerektiğinde maskelenmiş biçimde yazılır (ör. `+90******1234`).
- Loglara secret, parola, token veya tam kişisel veri yazılmaz — bu, log pipeline seviyesinde de (structured logging enricher/filter) uygulanır.
- Veri saklama ve silme kuralları modül bazında `docs/DATA_MODEL.md`'de belirtilecektir.
- Kullanıcı silme/anonimleştirme talepleri, ilişkili audit ve paket/randevu geçmişinin bütünlüğünü bozmayacak şekilde ele alınmalıdır (soft-delete + anonimleştirme kombinasyonu önerilir; kesin politika ilgili modül tasarım aşamasında netleştirilecektir).
