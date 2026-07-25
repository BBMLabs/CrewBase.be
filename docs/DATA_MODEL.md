# Veri Modeli

Bu doküman, platformun **tek** veri deposu olan PostgreSQL'deki tabloları, bu tabloların hangi modüle ait olduğunu, indekslerini ve EF Core mapping yaklaşımını özetler. MongoDB veya başka bir doküman veritabanı kullanılmaz — mimari kararın gerekçesi için bkz. [adr/0004-postgresql-tek-veritabani-stratejisi.md](./adr/0004-postgresql-tek-veritabani-stratejisi.md) (bkz. ayrıca [adr/0002-mongodb-tek-veritabani-stratejisi.md](./adr/0002-mongodb-tek-veritabani-stratejisi.md), artık **superseded**).

> **Durum notu:** Yalnızca **Identity** modülü implemente edilmektedir; aşağıdaki Identity tabloları gerçek koda (`InitialCreate` EF Core migration'ı) karşılık gelir. Diğer tüm modüllerin nesne/tablo listeleri, ana geliştirme talimatının §6 "Ana nesneler" bölümünden alınmış **planlanan/gösterge niteliğinde** tasarımlardır; ilgili modülün kendi tasarım geçişi yapılana kadar kesin şema olarak kabul edilmemelidir.

## Genel İlkeler

- **Tek mantıksal veritabanı**: `Postgres:DatabaseName` (`POSTGRES_DATABASE_NAME`, varsayılan `rowingclub`). Tüm modüller aynı veritabanı içinde, kendi tablolarına yazar.
- **Adlandırma kuralı**: modüle özel tablolar `<modül>_<aggregate>` biçiminde adlandırılır (ör. `identity_users`), böylece birden fazla modülün aggregate'lerini kapsayan bir transaction (ör. randevu + kontenjan + paket hakkı) tek Postgres veritabanı içinde, cross-database transaction yükü olmadan çalışabilir. Modül-agnostik paylaşılan tablolar: `outbox_messages`, `inbox_messages`.
- **Şema doğrulama**: her tablonun sütun tipleri, zorunluluğu (`NOT NULL`) ve indeksleri, ilgili modülün Infrastructure katmanındaki EF Core `IEntityTypeConfiguration<T>` sınıfları ile tanımlanır; `RowingClubDbContext.OnModelCreating`, her modülün kayıtlı `PersistenceAssemblyMarker`'ı üzerinden `ApplyConfigurationsFromAssembly` çağırarak bu konfigürasyonları toplar (bkz. [ARCHITECTURE.md §9](./ARCHITECTURE.md#9-postgresql-ve-ef-core-veri-modeli)).
- **İndeksler**: her modülün entity konfigürasyonu kendi tablosunun indekslerini de (unique/compound) tanımlar; bunlar EF Core migration'ları aracılığıyla veritabanına uygulanır.
- **Optimistic concurrency**: her aggregate satırı bir `version` sütunu taşır (bkz. [ARCHITECTURE.md §9](./ARCHITECTURE.md#9-postgresql-ve-ef-core-veri-modeli)); bu sütun iş şemasının bir parçası değildir, EF Core'un `IsConcurrencyToken()` ile işaretlediği ve persistence katmanının (`AggregateRoot.IncrementVersion()`) yönettiği teknik bir alandır.
- **Value object'ler ilişkisel sütunlara map'lenir**: ör. `EmailAddress` gibi bir value object, EF Core `OwnsOne`/`HasConversion` ile tek bir `Email` sütununa (text) map'lenir; ilgili unique index de bu sütun üzerine kurulur.

## EF Core Migrations

Tüm modüllerin entity mapping'leri, her modülün Infrastructure DI kaydının sunduğu bir `PersistenceAssemblyMarker(Assembly)` ile toplanır (`IMongoMigration`'ların eskiden toplandığı şekle benzer biçimde, artık DI ile); `RowingClubDbContext.OnModelCreating` her marker için `modelBuilder.ApplyConfigurationsFromAssembly(marker.Assembly)` çağırır. Migration'lar bunun üzerine EF Core Migrations ile üretilir ve `src/BuildingBlocks/RowingClub.BuildingBlocks.Infrastructure/Postgres/Migrations/` altında tutulur (ilki: `InitialCreate`).

Migration'lar, uygulama başlangıcında `EfMigrationHostedService` (`src/BuildingBlocks/RowingClub.BuildingBlocks.Infrastructure/Postgres/EfMigrationHostedService.cs`) tarafından `context.Database.MigrateAsync()` çağrısıyla otomatik uygulanır. Hangi migration'ların uygulandığı, EF Core'un kendi `__EFMigrationsHistory` tablosunda, dosya sırasına göre izlenir — artık Mongo'daki gibi modül başına ayrılmış manuel bir versiyon aralığı şeması (BuildingBlocks 1-99, Identity 100-199, ...) yoktur.

Yeni bir migration eklemek için:

```powershell
dotnet ef migrations add <Ad> `
  --project src/BuildingBlocks/RowingClub.BuildingBlocks.Infrastructure/RowingClub.BuildingBlocks.Infrastructure.csproj `
  --startup-project src/RowingClub.Api/RowingClub.Api.csproj `
  --context RowingClub.BuildingBlocks.Infrastructure.Postgres.RowingClubDbContext
```

Ayrıntılı adımlar için bkz. [DEVELOPMENT.md §4](./DEVELOPMENT.md#4-migration-çalıştırma).

## 1. Ortak / Modül-Agnostik Tablolar — **implemente edildi**

| Tablo | İçerik | İndeksler |
|---|---|---|
| `outbox_messages` | Aggregate'lerin ürettiği domain event'lerin, aggregate yazımıyla aynı transaction'da yazıldığı outbox kaydı (`Type`, `Content` — JSON serileştirilmiş event, `CorrelationId`, `OccurredOnUtc`, `ProcessedOnUtc`, `RetryCount`, `LastError`) | `ProcessedOnUtc` üzerinde non-unique index (henüz işlenmemiş mesajları taramak için) |
| `inbox_messages` | Bir consumer'ın işlediği event'lerin izini tutan idempotency kaydı (`EventId`, `ConsumerName`, `ProcessedAtUtc`) — henüz tüketen bir consumer yok (Notifications hâlâ scaffold), bu ilk consumer'ın kullanacağı şekil | `(EventId, ConsumerName)` üzerinde composite **primary key** — ayrı bir unique index'e gerek yok, PK'nin kendisi bunu zorlar |

## 2. Identity — **implemente edildi** (6 tablo)

| Tablo | İçerik | Önemli sütunlar | İndeksler |
|---|---|---|---|
| `identity_users` | Platform genelinde tekil kullanıcı kimliği: e-posta, e-posta doğrulama durumu, hesap durumu, başarısız giriş sayacı | `Id` (uuid, PK), `Email` (text), `EmailVerified` (bool), `Status` (text — enum, `HasConversion<string>()` ile string olarak saklanır), `FailedLoginAttemptCount` (int), `LockedUntilUtc` (timestamptz, nullable), `CreatedAtUtc` (timestamptz), `LastLoginAtUtc` (timestamptz, nullable), `Version` (bigint, concurrency token) | `Email` üzerinde **unique** |
| `identity_credentials` | Parola hash'i (Argon2id) ve hashleme parametreleri, kullanıcıya 1-1 bağlı | `UserId`, `PasswordHash`, `CreatedAtUtc`, `Version` | `UserId` üzerinde **unique** |
| `identity_refresh_tokens` | Hashlenmiş refresh token, token family id, oluşturulma/son kullanma zamanı — rotation ve reuse detection'ın veri kaynağı | `UserId`, `FamilyId`, `TokenHash`, `CreatedAtUtc`, `ExpiresAtUtc`, `Version` | `TokenHash` **unique**; `FamilyId` (non-unique); `UserId` (non-unique) |
| `identity_user_sessions` | Cihaz/oturum bilgisi, ilişkili refresh token family'si, son aktivite zamanı | `UserId`, `RefreshTokenFamilyId`, `CreatedAtUtc`, `LastSeenAtUtc`, `Version` | `RefreshTokenFamilyId` **unique**; `UserId` (non-unique) |
| `identity_email_verification_tokens` | E-posta doğrulama token'ı (hashlenmiş), son kullanma zamanı | `UserId`, `TokenHash`, `ExpiresAtUtc`, `Version` | `TokenHash` **unique**; `UserId` (non-unique) |
| `identity_password_reset_tokens` | Şifre sıfırlama token'ı (hashlenmiş), son kullanma zamanı | `UserId`, `TokenHash`, `ExpiresAtUtc`, `Version` | `TokenHash` **unique**; `UserId` (non-unique) |

Tüm sütun/tip eşlemeleri, eski Mongo doküman şekillerinden 1:1 taşınmıştır (ör. `email.value` üzerindeki eski unique Mongo index'i artık `identity_users.Email` üzerinde bir Postgres unique index'idir; `EnumRepresentationConvention(BsonType.String)` artık `HasConversion<string>()`'dır).

Identity'nin altı tablosu da `ITenantOwned` implemente **etmez** — Identity platform seviyesindedir, kulübe bağlı değildir (bir kullanıcı birden fazla kulübe üye olabilir); bkz. [ARCHITECTURE.md §9](./ARCHITECTURE.md#9-postgresql-ve-ef-core-veri-modeli).

## 3. Clubs — planlandı (gösterge niteliğinde)

| Tablo (öngörülen) | İçerik (gösterge niteliğinde) |
|---|---|
| `clubs_clubs` | Kulüp ana kaydı — tenant kök varlığı |
| `clubs_branches` | Kulübe bağlı şube/lokasyon |
| `clubs_settings` | Kulübe özel operasyonel ayarlar |
| `clubs_subscriptions` | Kulübün platform aboneliği (plan, durum, dönem) |

## 4. Memberships — planlandı (gösterge niteliğinde)

| Tablo (öngörülen) | İçerik (gösterge niteliğinde) |
|---|---|
| `memberships_club_memberships` | Kullanıcı-kulüp ilişkisi, üyelik durumu, başlangıç/bitiş tarihi |
| `memberships_invitations` | Üye daveti, davet token'ı, kabul durumu |
| `memberships_role_assignments` | Kullanıcının kulüp içi rol ataması |

`ClubMembership`, tenant çözümleme akışında "aktif üyelik doğrulama" adımının veri kaynağıdır (bkz. [ARCHITECTURE.md §7](./ARCHITECTURE.md#7-tenant-çözümleme-akışı)).

## 5. Scheduling — planlandı (gösterge niteliğinde)

| Tablo (öngörülen) | İçerik (gösterge niteliğinde) |
|---|---|
| `scheduling_lesson_types` | Ders tipi tanımı |
| `scheduling_lesson_sessions` | Planlanan ders seansı (tarih, eğitmen, kontenjan) |
| `scheduling_appointments` | Üyenin bir seansa randevusu |
| `scheduling_attendances` | Katılım/no-show kaydı |
| `scheduling_waitlist_entries` | Bekleme listesi kaydı |
| `scheduling_instructor_availabilities` | Eğitmen uygunluk takvimi |
| `scheduling_equipment_reservations` | Tekne/ekipman rezervasyonu |
| Ders notları (serbest metin) | Aynı `LessonSession`/`Appointment` tablosunun bir sütunu veya ayrı bir `scheduling_lesson_notes` tablosu olabilir — kesin tasarım Scheduling modülü implementasyonunda netleşecek |

Randevu/kontenjan/katılım işlemleri (ör. "kontenjan dolu değilse randevu oluştur") tek veritabanında `RowingClubDbContext.SaveChangesAsync` üzerinden aynı transaction'da birden fazla aggregate'i (`Appointment` + `LessonSession` kontenjan sayacı) EF Core'un change tracker'ıyla izleterek atomik biçimde ele alınabilir — bkz. [ARCHITECTURE.md §9](./ARCHITECTURE.md#9-postgresql-ve-ef-core-veri-modeli).

## 6. Packages — planlandı (gösterge niteliğinde)

| Tablo (öngörülen) | İçerik (gösterge niteliğinde) |
|---|---|
| `packages_definitions` | Kulübün tanımladığı ders paketi şablonu |
| `packages_member_packages` | Üyeye atanmış paket örneği (toplam/kullanılan/kalan hak, başlangıç/bitiş) |
| `packages_usage_transactions` | Immutable kullanım hareketi ledger'ı (her hak değişikliği bir kayıt üretir) |

Paket hakları asla negatife düşemez; her düzeltme/iade audit edilir ve `packages_usage_transactions` üzerinden iz bırakır (talimat §22). Bir randevu + paket hakkı düşümü, Scheduling ve Packages'ın aggregate'lerini aynı `SaveChangesAsync` transaction'ında yükleyip değiştirerek atomik yapılabilir.

## 7. Notifications — planlandı (gösterge niteliğinde)

| Tablo (öngörülen) | İçerik (gösterge niteliğinde) |
|---|---|
| `notifications_deliveries` | Push/e-posta/SMS içerik ve teslim durumu geçmişi |

`outbox_messages`'ı tüketecek ilk consumer bu modülde olacaktır; işlenmiş event ID'leri `inbox_messages`'a yazılarak idempotency sağlanır.

## 8. Reporting — planlandı (gösterge niteliğinde)

| Tablo (öngörülen) | İçerik (gösterge niteliğinde) |
|---|---|
| `reporting_snapshots` | Üye devamlılığı, doluluk oranı, paket satış/kullanım, eğitmen performansı gibi önceden hesaplanmış rapor snapshot'ları |

Ham işlemsel veri üzerinde kontrollü OData sorguları için bkz. [ODATA.md](./ODATA.md); tüm modüller artık aynı PostgreSQL veritabanında olduğundan OData sorgu katmanı tek bir veri kaynağına karşı çalışır.

## 9. Veri Saklama ve Silme Kuralları

- Genel ilke: kritik işlemsel veri (randevu, paket kullanım hareketi, audit) **immutable/append-only** mantıkla tutulur; silme yerine durum değişikliği (iptal, pasif, arşivlendi) tercih edilir.
- Kullanıcı hesabı silme talepleri soft-delete + ilişkili hassas alanların anonimleştirilmesi ile ele alınır; audit/finansal bütünlüğü bozacak sert (hard) silme yapılmaz.
- Süresi dolmuş token'lar (`identity_refresh_tokens`, `identity_email_verification_tokens`, `identity_password_reset_tokens`) periyodik temizleme job'ları ile arşivlenir/silinir; Postgres tarafında bu, zamanlanmış bir temizlik job'u (ör. `pg_cron` veya uygulama içi bir background worker) ile ele alınacaktır — Mongo'daki TTL index'in doğrudan bir Postgres eşdeğeri yoktur.
- Kesin saklama süreleri (retention period) ve KVKK/GDPR bazlı silme prosedürleri, ilgili modülün tasarım aşamasında ayrıca dokümante edilecektir.

## 10. İlişkiler ve İndeksler

Modüller henüz implemente edilmediği için tam ilişki/indeks listesi bu doküman kapsamında yalnızca Identity için verilmiştir (bkz. §2). Diğer modüller için tasarım geçişleri tamamlandıkça:

- İlgili modülün EF Core entity konfigürasyonuna dayalı gerçek tablo/index listesi,
- Modüller arası referans alanları (ör. `scheduling_appointments.MembershipId` → `memberships_club_memberships.Id`) — bunlar ilişkisel foreign key olarak modellenip modellenmeyeceği ilgili modülün tasarım aşamasında netleştirilecektir

bu bölüme sırayla eklenecektir. Diyagramlar `docs/diagrams/` altında tutulacaktır.
