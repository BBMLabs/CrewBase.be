# 0004 — PostgreSQL Tek Veritabanı Stratejisi

## Durum

Kabul edildi. **Bu ADR, [0002 — MongoDB Tek Veritabanı Stratejisi](./0002-mongodb-tek-veritabani-stratejisi.md) kararının yerine geçer (supersedes)** — MongoDB tamamen kaldırılmıştır, platform artık tek veri deposu olarak yalnızca PostgreSQL (EF Core üzerinden) kullanır. 0002 dosyası tarihsel kayıt olarak korunmuş, başına superseded notu eklenmiştir; yeni ADR numarası (0004) kullanılmıştır ki eski kararın bağlamı (Oracle+MongoDB → yalnızca MongoDB pivotu) kaybolmasın.

## Bağlam

Identity modülünün implementasyonu MongoDB üzerinde tamamlanmış, 37/37 test yeşil durumdayken, platformun kalıcılık katmanı MongoDB'den PostgreSQL + EF Core'a taşındı. Bu, `0002`'deki gibi dışarıdan gelen bir ürün talimatı değişikliği değil, kod tabanının kendi içinde önceden var olan tasarım izlerinin doğal sonucudur:

- `RowingClub.BuildingBlocks.Domain.Entity<TId>`'in parametresiz constructor'ı, en baştan `// EF Core materialization` yorumuyla yazılmıştı — MongoDB.Driver'ın kendi class-mapping convention'ları bu constructor'a ihtiyaç duymaz, yalnızca EF Core duyar.
- `RowingClub.BuildingBlocks.Domain.ITenantOwned`'ın XML doc yorumu, MongoDB implementasyonu yazılırken bile "EF Core global query filter" ve "SaveChanges" dilini kullanıyordu — yani tenant izolasyon mekanizmasının nihai hedefi baştan beri EF Core'un sağladığı bu ikiliydi (bkz. [adr/0003-tenant-izolasyon-stratejisi.md](./0003-tenant-izolasyon-stratejisi.md); mevcut implementasyonda henüz bir global query filter yoktur, yalnızca `SaveChanges`-zamanı doğrulama vardır — bkz. aşağıdaki Karar bölümü).
- `OutboxMessage` sınıfı, Mongo-native hale getirilmeden önce zaten bir EF konfigürasyonu taşıyordu (bkz. eski `0002` ADR'sinin "Outbox/Inbox Mongo-native" maddesi, "EF konfigürasyonu kaldırıldı" notu) — yani orijinal tasarım EF Core'du, MongoDB'ye geçiş aradaki bir sapmaydı.

Buna ek olarak, MongoDB tasarımının pratikte biriken somut sürtünme noktaları vardı:

1. **Replica set zorunluluğu.** MongoDB'nin multi-document transaction'ları yalnızca bir replica set'e karşı çalışır; standalone `mongod` desteklemez. Bu, lokal geliştirme ve test altyapısına (`--replSet rs0` ile başlayan bir Docker servisi, healthcheck'te idempotent `rs.initiate()`) sürekli bir karmaşıklık ekliyordu — tek node'da bile "gerçek" bir replica set kurulumu gerekiyordu.
2. **Manuel track-on-read deseni.** MongoDB.Driver'ın EF Core benzeri bir change tracker'ı olmadığından, her `GetByIdAsync`/`Add` çağrısının sonucu `MongoUnitOfWork.Track()` ile elle izlemeye alınması gerekiyordu; bu adımı unutmak, mutasyonun `SaveChangesAsync`'te sessizce kaybolmasına yol açabiliyordu — yapısal olarak unutulması mümkün bir adımdı.
3. **Versiyonlu migration sisteminin elle bakımı.** `IMongoMigration` + modül başına ayrılmış versiyon aralığı (BuildingBlocks 1-99, Identity 100-199, ...) şeması, EF Core Migrations'ın sağladığı olgun tooling'in (otomatik diff üretme, `dotnet ef migrations add`, `__EFMigrationsHistory`) elle yeniden inşa edilmiş, daha kısıtlı bir versiyonuydu.
4. **`$jsonSchema` validator'larının sınırlı disiplini.** Yalnızca zorunlu alanları kilitleyen, tip/şekil doğrulaması yapmayan bir şema disiplini, ilişkisel bir veritabanının sağladığı sütun tipi/`NOT NULL`/foreign key zorlamasının yerini tutmuyordu.

## Karar

RowingClub, **tek veri deposu olarak yalnızca PostgreSQL** kullanır, EF Core (`Npgsql.EntityFrameworkCore.PostgreSQL`) üzerinden erişilir. MongoDB veya başka bir doküman veritabanı mimaride yer almaz.

Somut kararlar:

1. **Tek paylaşılan `DbContext`**: `RowingClub.BuildingBlocks.Infrastructure.Postgres.RowingClubDbContext`. Modül entity mapping'leri, her modülün Infrastructure DI kaydının DI konteynerine sunduğu bir `PersistenceAssemblyMarker(Assembly)` ile toplanır (`IMongoMigration`'ların DI ile toplanma şekline benzer, yeni sisteme taşınmış hali); `OnModelCreating`, her marker için `ApplyConfigurationsFromAssembly` çağırır. `RowingClub.BuildingBlocks.Infrastructure` hiçbir modüle doğrudan referans vermez.
2. **`SaveChangesAsync` tek transaction içinde üç işi birden yapar**: tenant sahiplik doğrulaması (`ITenantOwned.ClubId` vs `ICurrentTenant.ClubId`, uyuşmazlıkta `DomainException("tenant_mismatch", ...)`), domain event'lerin `OutboxMessages`'a drenajı (aynı `SaveChanges` çağrısı içinde, ayrı bir distributed transaction'a gerek kalmadan) ve `AggregateRoot<TId>.Version` artırımı — hepsi EF'in kendi `SaveChanges` transaction'ı içinde, açık bir BEGIN/COMMIT yönetmeye gerek kalmadan.
3. **Optimistic concurrency EF'in kendi mekanizmasıyla**: `AggregateRoot<TId>.Version` (long), EF Core'da `IsConcurrencyToken()` ile işaretlenir; EF, UPDATE'lere otomatik `WHERE version = @original` ekler. Eşleşme yoksa `DbUpdateConcurrencyException` fırlatılır, `RowingClubDbContext` bunu yakalayıp aynı `ConcurrencyException(entityName, id)` domain hatasına çevirir (→ HTTP 409) — davranış MongoDB tasarımıyla aynıdır, yalnızca mekanizma değişmiştir.
4. **EF Core Migrations**: `IMongoMigration`/`MongoMigrationRunner`/`MongoMigrationHostedService` sistemi tamamen kaldırılmıştır. Migration'lar `src/BuildingBlocks/RowingClub.BuildingBlocks.Infrastructure/Postgres/Migrations/` altında tutulur (ilki `InitialCreate`), uygulama başlangıcında `EfMigrationHostedService`'in çağırdığı `context.Database.MigrateAsync()` ile otomatik uygulanır. Uygulanan migration'lar EF'in kendi `__EFMigrationsHistory` tablosunda, dosya sırasına göre izlenir — modül başına ayrılmış manuel versiyon aralığı şemasına artık gerek yoktur.
5. **Config**: `POSTGRES_HOST`/`POSTGRES_PORT`/`POSTGRES_DATABASE_NAME`/`POSTGRES_USERNAME`/`POSTGRES_PASSWORD` env değişkenleri (`Postgres:*` config anahtarlarına map'lenir), `MONGODB_*` değişkenlerinin yerini alır. Bağlantı stringi parçalı ayarlardan (`PostgresOptions.ConnectionString`) inşa edilir; `replicaSet=rs0` gibi bir gereklilik yoktur.
6. **Repository adlandırması ve change tracking**: `Mongo*Repository` sınıfları (ör. `MongoUserRepository`) `Mongo` önekini kaybederek sadeleşti (ör. `UserRepository`) — artık tek bir persistence teknolojisi olduğu için önek anlamsızlaştı. Eski manuel "Track()" adımına gerek yoktur; EF Core'un ChangeTracker'ı, bir entity `RowingClubDbContext` üzerinden bir kez yüklendiğinde onu otomatik izler.
7. **Tenant izolasyonu**: tenant sahiplik kontrolü artık `RowingClubDbContext.SaveChangesAsync`'te uygulanır (bkz. madde 2) — yazma anında uygulanan bir kontroldür. Şu an `ClubId` üzerinde global bir EF Core query filter'ı **yoktur**; bu, ileride değerlendirilebilecek ayrı bir iyileştirmedir (bkz. [adr/0003-tenant-izolasyon-stratejisi.md](./0003-tenant-izolasyon-stratejisi.md)).
8. **Test altyapısı**: `MongoIdentityFixture` (Testcontainers.MongoDb tabanlı), `.env.developer`'daki ile aynı uzak dev Postgres sunucusuna bağlanan, Docker/Testcontainers gerektirmeyen `PostgresIdentityFixture`'a dönüştü — testler `rowingclub_tests` adlı ayrı bir veritabanını her koşuda drop+create edip gerçek EF Core migration'larıyla migrate eder, böylece test şeması hiçbir zaman production'dan sapmaz.

## Sonuçlar

**Olumlu:**

- Gerçek ACID transaction desteği tek node'da bile mevcuttur; MongoDB'nin replica set zorunluluğu (lokal geliştirme ve testlerde sürekli sürtünme kaynağıydı) tamamen ortadan kalkar.
- EF Core'un olgun migration tooling'i (`dotnet ef migrations add`, otomatik model-diff üretimi, `__EFMigrationsHistory`) devreye girer; modül başına elle ayrılmış versiyon aralığı şemasına artık gerek yoktur.
- Manuel track-on-read deseni ortadan kalkar; EF Core'un ChangeTracker'ı entity'leri otomatik izler, bu adımın unutulma riski yapısal olarak yok olur.
- İlişkisel şema (sütun tipleri, `NOT NULL`, unique/foreign key kısıtları) `$jsonSchema`'nın yalnızca zorunlu alanları kilitleyen gevşek disiplininden daha güçlü bir bütünlük garantisi sağlar.
- Kod tabanındaki önceden var olan EF Core izlerinin (Entity'nin parametresiz constructor'ı, ITenantOwned'ın doc yorumu, OutboxMessage'ın eski EF konfigürasyonu) tutarlı biçimde tamamlanmış olması — mimari artık kodun baştan beri işaret ettiği yöne hizalanmıştır.

**Olumsuz / riskler:**

- Şema artık ilişkiseldir: yeni bir sütun eklemek bir migration gerektirir; MongoDB'nin şemasız doküman esnekliği (var olan dokümanları etkilemeden yeni alan ekleme) kaybedilir.
- Value object'lerin (ör. `EmailAddress`) ilişkisel sütunlara `HasConversion`/`OwnsOne` ile map'lenmesi, MongoDB'nin otomatik class-mapping convention'larına göre biraz daha fazla elle yazılmış konfigürasyon gerektirir.
- `ClubId` üzerinde henüz global bir EF Core query filter'ı yoktur; tenant izolasyonu yalnızca yazma anında (`SaveChangesAsync`) zorlanır, okuma yolunda repository sorgularının kendisine bağlıdır — bu, ileride ele alınması gereken bir iyileştirme alanıdır.
- Npgsql/EF Core için henüz proje içinde wire edilmiş bir OpenTelemetry instrumentation paketi yoktur; Postgres sorgu span'leri şu an izlenmiyor (bkz. [OBSERVABILITY.md](../OBSERVABILITY.md)) — MongoDB.Driver'daki aynı boşluğun PostgreSQL'e taşınmış hali.

## İlgili

- [DATA_MODEL.md](../DATA_MODEL.md) — tablo/indeks/EF Core migration detayları
- [ARCHITECTURE.md §8-9](../ARCHITECTURE.md#8-outboxinbox-akışı) — outbox akışı ve PostgreSQL/EF Core veri modeli
- [adr/0002-mongodb-tek-veritabani-stratejisi.md](./0002-mongodb-tek-veritabani-stratejisi.md) — superseded, tarihsel bağlam için
- [adr/0003-tenant-izolasyon-stratejisi.md](./0003-tenant-izolasyon-stratejisi.md) — tenant izolasyonunun EF Core-native karşılığı
- `src/BuildingBlocks/RowingClub.BuildingBlocks.Infrastructure/Postgres/` — `RowingClubDbContext`, `PostgresOptions`, `EfMigrationHostedService`, `PostgresHealthCheck`
