# Veri Modeli

Bu doküman, platformun **tek** veri deposu olan MongoDB'deki koleksiyonları, bu koleksiyonların hangi modüle ait olduğunu, indekslerini ve `$jsonSchema` doğrulama yaklaşımını özetler. Oracle veya başka bir ilişkisel veritabanı kullanılmaz — mimari kararın gerekçesi için bkz. [adr/0002-mongodb-tek-veritabani-stratejisi.md](./adr/0002-mongodb-tek-veritabani-stratejisi.md).

> **Durum notu:** Yalnızca **Identity** modülü implemente edilmektedir; aşağıdaki Identity koleksiyonları gerçek koda (`IdentityCollectionsMigration`) karşılık gelir. Diğer tüm modüllerin nesne/koleksiyon listeleri, ana geliştirme talimatının §6 "Ana nesneler" bölümünden alınmış **planlanan/gösterge niteliğinde** tasarımlardır; ilgili modülün kendi tasarım geçişi yapılana kadar kesin şema olarak kabul edilmemelidir.

## Genel İlkeler

- **Tek mantıksal veritabanı**: `Mongo:DatabaseName` (`MONGODB_DATABASE_NAME`, varsayılan `rowingclub`). Tüm modüller aynı veritabanı içinde, kendi koleksiyonlarına yazar.
- **Adlandırma kuralı**: modüle özel koleksiyonlar `<modül>_<aggregate>` biçiminde adlandırılır (ör. `identity_users`), böylece birden fazla modülün aggregate'lerini kapsayan bir transaction (ör. randevu + kontenjan + paket hakkı) tek replica set içinde, cross-database transaction yükü olmadan çalışabilir. Modül-agnostik paylaşılan koleksiyonlar: `outbox_messages`, `inbox_messages`, `schema_migrations`.
- **Şema doğrulama**: her koleksiyon, oluşturulurken bir `$jsonSchema` validator'ı alır — yalnızca zorunlu alanları (`required`) tanımlar, alan tiplerini/şeklini katı biçimde kilitlemez. Bu, ilişkisel bir şemanın disiplinini bir miktar korurken MongoDB'nin doküman esnekliğini feda etmez.
- **İndeksler**: her migration kendi koleksiyonunun indekslerini de kurar (unique/compound). Migration'lar idempotent'tir — koleksiyon zaten varsa yeniden oluşturulmaz.
- **Optimistic concurrency**: her aggregate dokümanı bir `version` alanı taşır (bkz. [ARCHITECTURE.md §9.4](./ARCHITECTURE.md#9-mongodb-veri-modeli)); bu alan iş şemasının bir parçası değildir, persistence katmanının (`AggregateRoot.IncrementVersion()`) yönettiği teknik bir alandır.
- **Value object'ler nested subdocument'tır**: ör. `EmailAddress` gibi bir value object `{ "email": { "value": "..." } }` şeklinde serialize olur; ilgili unique index de `email.value` yolu üzerine kurulur (`email` üzerine değil) — bkz. [ARCHITECTURE.md §9.10](./ARCHITECTURE.md#9-mongodb-veri-modeli).

## Migration Versiyon Aralıkları

Tüm modüllerin `IMongoMigration`'ları başlangıçta tek, global sıralı bir listede birleştirilip (`Version`'a göre) sırayla çalıştırılır; çakışmayı önlemek için modül başına aralık ayrılmıştır:

| Modül | Versiyon Aralığı | Durum |
|---|---:|---|
| BuildingBlocks (ortak: outbox/inbox indeksleri) | 1-99 | implemente edildi (`CoreCollectionsMigration`, v1) |
| Identity | 100-199 | implemente edildi (`IdentityCollectionsMigration`, v100) |
| Clubs | 200-299 | planlandı |
| Memberships | 300-399 | planlandı |
| Scheduling | 400-499 | planlandı |
| Packages | 500-599 | planlandı |
| Notifications | 600-699 | planlandı |
| Reporting | 700-799 | planlandı |

## 1. Ortak / Modül-Agnostik Koleksiyonlar — **implemente edildi**

| Koleksiyon | İçerik | İndeksler |
|---|---|---|
| `outbox_messages` | Aggregate'lerin ürettiği domain event'lerin, aggregate yazımıyla aynı transaction'da yazıldığı outbox kaydı (`Type`, `Content` — JSON serileştirilmiş event, `CorrelationId`, `OccurredOnUtc`, `ProcessedOnUtc`, `RetryCount`, `LastError`) | `ProcessedOnUtc` üzerinde ascending (henüz işlenmemiş mesajları taramak için) |
| `inbox_messages` | Bir consumer'ın işlediği event'lerin izini tutan idempotency kaydı (`EventId`, `ConsumerName`, `ProcessedAtUtc`) — henüz tüketen bir consumer yok (Notifications hâlâ scaffold), bu ilk consumer'ın kullanacağı şekil | `(EventId, ConsumerName)` üzerinde unique compound |
| `schema_migrations` | `MongoMigrationRunner`'ın uyguladığı migration'ların kaydı (`Version`, `Name`, `AppliedAtUtc`) — yeniden deploy'da hangi migration'ların atlanacağını belirler | — |

## 2. Identity — **implemente edildi** (6 koleksiyon)

| Koleksiyon | İçerik | Zorunlu alanlar (`$jsonSchema`) | İndeksler |
|---|---|---|---|
| `identity_users` | Platform genelinde tekil kullanıcı kimliği: e-posta (nested `email.value`), e-posta doğrulama durumu, hesap durumu, başarısız giriş sayacı | `email`, `emailVerified`, `status`, `failedLoginAttemptCount`, `createdAtUtc`, `version` | `email.value` üzerinde **unique** |
| `identity_credentials` | Parola hash'i (Argon2id) ve hashleme parametreleri, kullanıcıya 1-1 bağlı | `userId`, `passwordHash`, `createdAtUtc`, `version` | `userId` üzerinde **unique** |
| `identity_refresh_tokens` | Hashlenmiş refresh token, token family id, oluşturulma/son kullanma zamanı — rotation ve reuse detection'ın veri kaynağı | `userId`, `familyId`, `tokenHash`, `createdAtUtc`, `expiresAtUtc`, `version` | `tokenHash` **unique**; `familyId` (non-unique); `userId` (non-unique) |
| `identity_user_sessions` | Cihaz/oturum bilgisi, ilişkili refresh token family'si, son aktivite zamanı | `userId`, `refreshTokenFamilyId`, `createdAtUtc`, `lastSeenAtUtc`, `version` | `refreshTokenFamilyId` **unique**; `userId` (non-unique) |
| `identity_email_verification_tokens` | E-posta doğrulama token'ı (hashlenmiş), son kullanma zamanı | `userId`, `tokenHash`, `expiresAtUtc`, `version` | `tokenHash` **unique**; `userId` (non-unique) |
| `identity_password_reset_tokens` | Şifre sıfırlama token'ı (hashlenmiş), son kullanma zamanı | `userId`, `tokenHash`, `expiresAtUtc`, `version` | `tokenHash` **unique**; `userId` (non-unique) |

Identity'nin altı koleksiyonu da `ITenantOwned` implemente **etmez** — Identity platform seviyesindedir, kulübe bağlı değildir (bir kullanıcı birden fazla kulübe üye olabilir); bkz. [ARCHITECTURE.md §9.7](./ARCHITECTURE.md#9-mongodb-veri-modeli).

## 3. Clubs — planlandı (gösterge niteliğinde)

| Koleksiyon (öngörülen) | İçerik (gösterge niteliğinde) |
|---|---|
| `clubs_clubs` | Kulüp ana kaydı — tenant kök varlığı |
| `clubs_branches` | Kulübe bağlı şube/lokasyon |
| `clubs_settings` | Kulübe özel operasyonel ayarlar (esnek/dinamik alanlar dahil — ayrı bir Oracle/Mongo ayrımına artık gerek yok, hepsi aynı dokümanda tutulabilir) |
| `clubs_subscriptions` | Kulübün platform aboneliği (plan, durum, dönem) |

## 4. Memberships — planlandı (gösterge niteliğinde)

| Koleksiyon (öngörülen) | İçerik (gösterge niteliğinde) |
|---|---|
| `memberships_club_memberships` | Kullanıcı-kulüp ilişkisi, üyelik durumu, başlangıç/bitiş tarihi |
| `memberships_invitations` | Üye daveti, davet token'ı, kabul durumu |
| `memberships_role_assignments` | Kullanıcının kulüp içi rol ataması |

`ClubMembership`, tenant çözümleme akışında "aktif üyelik doğrulama" adımının veri kaynağıdır (bkz. [ARCHITECTURE.md §7](./ARCHITECTURE.md#7-tenant-çözümleme-akışı)).

## 5. Scheduling — planlandı (gösterge niteliğinde)

| Koleksiyon (öngörülen) | İçerik (gösterge niteliğinde) |
|---|---|
| `scheduling_lesson_types` | Ders tipi tanımı |
| `scheduling_lesson_sessions` | Planlanan ders seansı (tarih, eğitmen, kontenjan) |
| `scheduling_appointments` | Üyenin bir seansa randevusu |
| `scheduling_attendances` | Katılım/no-show kaydı |
| `scheduling_waitlist_entries` | Bekleme listesi kaydı |
| `scheduling_instructor_availabilities` | Eğitmen uygunluk takvimi |
| `scheduling_equipment_reservations` | Tekne/ekipman rezervasyonu |
| Ders notları (serbest metin) | Aynı `LessonSession`/`Appointment` dokümanının bir alt alanı veya ayrı bir `scheduling_lesson_notes` koleksiyonu olabilir — kesin tasarım Scheduling modülü implementasyonunda netleşecek |

Randevu/kontenjan/katılım işlemleri (ör. "kontenjan dolu değilse randevu oluştur") tek veritabanında `IMongoUnitOfWork` üzerinden aynı transaction'da birden fazla aggregate'i (`Appointment` + `LessonSession` kontenjan sayacı) `Track()` ederek atomik biçimde ele alınabilir — bkz. [ARCHITECTURE.md §9.8](./ARCHITECTURE.md#9-mongodb-veri-modeli).

## 6. Packages — planlandı (gösterge niteliğinde)

| Koleksiyon (öngörülen) | İçerik (gösterge niteliğinde) |
|---|---|
| `packages_definitions` | Kulübün tanımladığı ders paketi şablonu |
| `packages_member_packages` | Üyeye atanmış paket örneği (toplam/kullanılan/kalan hak, başlangıç/bitiş) |
| `packages_usage_transactions` | Immutable kullanım hareketi ledger'ı (her hak değişikliği bir kayıt üretir) |

Paket hakları asla negatife düşemez; her düzeltme/iade audit edilir ve `packages_usage_transactions` üzerinden iz bırakır (talimat §22). Bir randevu + paket hakkı düşümü, Scheduling ve Packages'ın aggregate'lerini aynı `SaveChangesAsync` transaction'ında `Track()` ederek atomik yapılabilir.

## 7. Notifications — planlandı (gösterge niteliğinde)

| Koleksiyon (öngörülen) | İçerik (gösterge niteliğinde) |
|---|---|
| `notifications_deliveries` | Push/e-posta/SMS içerik ve teslim durumu geçmişi |

`outbox_messages`'ı tüketecek ilk consumer bu modülde olacaktır; işlenmiş event ID'leri `inbox_messages`'a yazılarak idempotency sağlanır.

## 8. Reporting — planlandı (gösterge niteliğinde)

| Koleksiyon (öngörülen) | İçerik (gösterge niteliğinde) |
|---|---|
| `reporting_snapshots` | Üye devamlılığı, doluluk oranı, paket satış/kullanım, eğitmen performansı gibi önceden hesaplanmış rapor snapshot'ları |

Ham işlemsel veri üzerinde kontrollü OData sorguları için bkz. [ODATA.md](./ODATA.md); tüm modüller artık aynı MongoDB veritabanında olduğundan OData sorgu katmanı tek bir veri kaynağına karşı çalışır.

## 9. Veri Saklama ve Silme Kuralları

- Genel ilke: kritik işlemsel veri (randevu, paket kullanım hareketi, audit) **immutable/append-only** mantıkla tutulur; silme yerine durum değişikliği (iptal, pasif, arşivlendi) tercih edilir.
- Kullanıcı hesabı silme talepleri soft-delete + ilişkili hassas alanların anonimleştirilmesi ile ele alınır; audit/finansal bütünlüğü bozacak sert (hard) silme yapılmaz.
- Süresi dolmuş token'lar (`identity_refresh_tokens`, `identity_email_verification_tokens`, `identity_password_reset_tokens`) periyodik temizleme job'ları ile arşivlenir/silinir; MongoDB'nin TTL index özelliği bu temizlik için doğal bir aday olarak değerlendirilecektir.
- Kesin saklama süreleri (retention period) ve KVKK/GDPR bazlı silme prosedürleri, ilgili modülün tasarım aşamasında ayrıca dokümante edilecektir.

## 10. İlişkiler ve İndeksler

Modüller henüz implemente edilmediği için tam ilişki/indeks listesi bu doküman kapsamında yalnızca Identity için verilmiştir (bkz. §2). Diğer modüller için tasarım geçişleri tamamlandıkça:

- İlgili modülün `IMongoMigration` implementasyonuna dayalı gerçek koleksiyon/index listesi,
- Modüller arası referans alanları (ör. `scheduling_appointments.membershipId` → `memberships_club_memberships._id`) — MongoDB'de foreign key zorlaması yoktur, bu referanslar yalnızca uygulama seviyesinde doğrulanır

bu bölüme sırayla eklenecektir. Diyagramlar `docs/diagrams/` altında tutulacaktır.
