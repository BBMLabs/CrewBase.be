# Değişiklik Günlüğü

Bu dosya [Keep a Changelog](https://keepachangelog.com/) formatını takip eder. Sürümler [Semantic Versioning](https://semver.org/) ile uyumludur.

## [Yayınlanmamış]

### Eklenenler

- **Kalıcılık katmanı MongoDB'den PostgreSQL + EF Core'a taşındı** (bkz. [adr/0004-postgresql-tek-veritabani-stratejisi.md](./adr/0004-postgresql-tek-veritabani-stratejisi.md) — [adr/0002-mongodb-tek-veritabani-stratejisi.md](./adr/0002-mongodb-tek-veritabani-stratejisi.md)'yi supersede eder). Tüm kod ve testler geçirildi, 37/37 test yeşil:
  - `RowingClub.BuildingBlocks.Infrastructure.Postgres`: tek paylaşılan `RowingClubDbContext` (`Npgsql.EntityFrameworkCore.PostgreSQL` üzerinden), `PostgresOptions` (`POSTGRES_HOST`/`POSTGRES_PORT`/`POSTGRES_DATABASE_NAME`/`POSTGRES_USERNAME`/`POSTGRES_PASSWORD`'dan Npgsql bağlantı stringi inşa eder), `PostgresHealthCheck` (`SELECT 1`), `EfMigrationHostedService` (`context.Database.MigrateAsync()` ile startup'ta otomatik migration).
  - Modül entity mapping'leri artık `PersistenceAssemblyMarker(Assembly)` ile DI üzerinden toplanıyor; `RowingClubDbContext.OnModelCreating` her marker için `ApplyConfigurationsFromAssembly` çağırıyor.
  - `RowingClubDbContext.SaveChangesAsync`, eski `MongoUnitOfWork`'ün yaptığı üç işi (tenant sahiplik doğrulama, domain event'leri outbox'a drenaj, `AggregateRoot.Version` artırımı) tek bir EF Core `SaveChanges` transaction'ı içinde yapıyor; ayrı bir BEGIN/COMMIT'e gerek yok.
  - Optimistic concurrency artık EF Core'un `IsConcurrencyToken()`'ı üzerinden: `DbUpdateConcurrencyException`, `RowingClubDbContext` tarafından yakalanıp aynı `ConcurrencyException` domain hatasına çevriliyor (→ HTTP 409, davranış değişmedi).
  - `IMongoMigration`/`MongoMigrationRunner`/`MongoMigrationHostedService` tamamen kaldırıldı; yerini EF Core Migrations aldı (`src/BuildingBlocks/RowingClub.BuildingBlocks.Infrastructure/Postgres/Migrations/`, ilk migration `InitialCreate`). Modül başına ayrılmış manuel versiyon aralığı şemasına (BuildingBlocks 1-99, Identity 100-199, ...) artık gerek yok; EF'in kendi `__EFMigrationsHistory` tablosu izliyor.
  - **Identity modülünün 6 repository'si** (`Mongo` önekleri düşürülerek, ör. `MongoUserRepository` → `UserRepository`) `RowingClubDbContext` üzerine yeniden yazıldı; EF Core'un ChangeTracker'ı sayesinde eski manuel "Track()" adımına gerek kalmadı.
  - 6 Identity tablosu (`identity_users`, `identity_credentials`, `identity_refresh_tokens`, `identity_user_sessions`, `identity_email_verification_tokens`, `identity_password_reset_tokens`) ve `outbox_messages`/`inbox_messages`, eski Mongo koleksiyon şekillerinden 1:1 taşınan sütun/indekslerle `InitialCreate` migration'ında oluşturuluyor.
  - `EnvironmentConfigurationExtensions`'daki `MONGODB_*` eşlemeleri `POSTGRES_HOST`/`POSTGRES_PORT`/`POSTGRES_DATABASE_NAME`/`POSTGRES_USERNAME`/`POSTGRES_PASSWORD` → `Postgres:*` eşlemeleriyle değiştirildi.
  - Integration testleri (`MongoIdentityFixture` → `PostgresIdentityFixture`) artık Testcontainers/Docker kullanmıyor; `.env.developer`'daki ile aynı uzak dev PostgreSQL sunucusuna bağlanıp ayrı bir `rowingclub_tests` veritabanını her koşuda drop+create edip gerçek EF Core migration'larıyla migrate ediyor — test şeması hiçbir zaman production'dan sapmıyor.
  - `DotEnvFileLoader`, `RowingClub.Api.Configuration`'dan `RowingClub.BuildingBlocks.Infrastructure.Configuration`'a taşındı.
  - `docs/` altındaki ilgili dokümanlar (`DATA_MODEL.md`, `ARCHITECTURE.md`, `SECURITY.md`, `DEPLOYMENT.md`, `DEVELOPMENT.md`, `OBSERVABILITY.md`, `API_ENDPOINTS.md`, `README.md`) Postgres/EF Core terminolojisine güncellendi; yeni [adr/0004](./adr/0004-postgresql-tek-veritabani-stratejisi.md) eklendi, [adr/0002](./adr/0002-mongodb-tek-veritabani-stratejisi.md) superseded olarak işaretlendi.
- Modüler monolith çözüm iskeleti oluşturuldu: `RowingClub.sln`, `src/RowingClub.Api`, `src/RowingClub.Bootstrapper`, 5 `BuildingBlocks` kütüphanesi (`Domain`, `Application`, `Infrastructure`, `Security`, `Observability`).
- Altı modülün ({Identity, Clubs, Memberships, Scheduling, Packages, Notifications}) `Domain`/`Application`/`Infrastructure`/`Contracts` proje iskeletleri ve `Reporting` modülünün `Application`/`Infrastructure` proje iskeletleri oluşturuldu.
- Beş test projesi (`UnitTests`, `IntegrationTests`, `FunctionalTests`, `ArchitectureTests`, `SecurityTests`) iskeleti oluşturuldu.
- Katmanlar arası referans grafiği (Domain → bağımsız, Application → Domain, Infrastructure → Application+Domain, Api → yalnızca Bootstrapper, Bootstrapper → tüm modüller) kuruldu ve derleniyor.
- `docs/` altında temel proje dokümantasyonu (README, DEVELOPMENT, ARCHITECTURE, SECURITY, DATA_MODEL, ODATA, OBSERVABILITY, DEPLOYMENT) ve ilk üç ADR eklendi.
- **Identity modülü implementasyonu başladı**: kullanıcı kaydı, giriş, refresh token rotation (reuse detection ile), logout, logout-all akışları geliştirme aşamasında (`/api/v1/auth/*`).
- **Mimari, Oracle+MongoDB ikili veritabanından yalnızca-MongoDB tek veritabanına çevrildi** (açık ürün kararı — bkz. [adr/0002-mongodb-tek-veritabani-stratejisi.md](./adr/0002-mongodb-tek-veritabani-stratejisi.md)). Kalıcılık katmanı baştan yazıldı:
  - `RowingClub.BuildingBlocks.Infrastructure.Mongo`: `MongoOptions`, `MongoBsonConfiguration` (Guid/enum/camelCase BSON convention'ları), `IMongoUnitOfWork`/`MongoUnitOfWork` (track-on-read deseni, whole-document replace + optimistic concurrency + aynı transaction'da outbox yazımı), `Migrations/IMongoMigration` + `MongoMigrationRunner` + `MongoMigrationHostedService` (versiyonlu, otomatik, startup'ta çalışan migration'lar; `schema_migrations` koleksiyonunda izlenir).
  - `AggregateRoot<TId>`'e optimistic concurrency için `Version` alanı ve `IncrementVersion()` eklendi; versiyon uyuşmazlığında `ConcurrencyException` (→ HTTP 409).
  - `OutboxMessage` Mongo-native hale getirildi (EF konfigürasyonu kaldırıldı); ileride idempotent consumer'ların kullanacağı `InboxMessage` (`eventId`+`consumerName` unique index) eklendi.
  - **Identity modülü MongoDB üzerine yeniden yazıldı**: 6 koleksiyon (`identity_users`, `identity_credentials`, `identity_refresh_tokens`, `identity_user_sessions`, `identity_email_verification_tokens`, `identity_password_reset_tokens`), her biri `$jsonSchema` validator'ı ve unique/compound indeksleriyle (`IdentityCollectionsMigration`, versiyon 100); 6 Mongo repository implementasyonu track-on-read deseniyle yazıldı.
  - `EnvironmentConfigurationExtensions`'daki Oracle'a özgü env var eşlemeleri kaldırıldı; `MONGODB_CONNECTION_STRING`/`MONGODB_DATABASE_NAME` tek veri deposu bağlantısı olarak kaldı.
  - `deploy/docker/docker-compose.yml`'e tek bir `mongodb` servisi eklendi (`--replSet rs0`, ilk açılışta idempotent replica set initiate eden healthcheck); `oracle` servisi hiç eklenmedi.
  - Integration testleri (`MongoIdentityFixture`, `UserRepositoryTests`) Testcontainers.MongoDb ile gerçek bir replica set'e karşı yazıldı; optimistic-concurrency senaryosu (`Concurrent_writers_racing_on_the_same_user_throws_ConcurrencyException_for_the_loser`) canlı olarak doğrulandı.

### Değişen Endpoint'ler

- Henüz yayınlanmış/stabilize edilmiş bir endpoint sözleşmesi yok; Identity endpoint'leri geliştirme aşamasında olduğu için `docs/API_ENDPOINTS.md` ve `docs/AUTHORIZATION_MATRIX.md` henüz oluşturulmadı — Identity implementasyonu tamamlandığında eklenecek.

### Kırıcı Değişiklikler

- Mimari pivotu: Oracle tamamen kaldırıldı, platform artık yalnızca MongoDB kullanıyor. Bu, ilk iskelet aşamasında (henüz hiçbir Oracle şeması/veri üretilmediği için) geriye dönük bir veri geçişi gerektirmedi, ancak `docs/adr/0002-oracle-ve-mongodb-birlikte-kullanim.md` artık geçersizdir — yerine `docs/adr/0002-mongodb-tek-veritabani-stratejisi.md` eklendi (supersedes).

### Migration Değişiklikleri

- İlk Mongo migration'ları eklendi: `CoreCollectionsMigration` (versiyon 1 — `outbox_messages`/`inbox_messages` indeksleri), `IdentityCollectionsMigration` (versiyon 100 — Identity'nin 6 koleksiyonu, şema validator'ları ve indeksleri). Migration'lar artık `MongoMigrationHostedService` ile API başlangıcında otomatik çalışır, manuel bir komut gerekmez.

### Güvenlik Değişiklikleri

- Güvenlik mimarisi (JWT access/refresh, alan bazlı AES-256-GCM şifreleme, secret yönetimi, tenant izolasyonu) dokümante edildi (`docs/SECURITY.md`); implementasyon Identity modülüyle birlikte ilerliyor.
- Tenant izolasyonu artık `MongoUnitOfWork.Track()` içinde `ITenantOwned` kontrolüyle sağlanıyor (EF Core global query filter yerine) — bkz. `docs/adr/0003-tenant-izolasyon-stratejisi.md`.

---

## Sürüm Notları Hakkında

Her yeni özellik/PR tamamlandığında bu dosyaya bir giriş eklenir. Bir sürüm etiketlendiğinde `[Yayınlanmamış]` bölümündeki maddeler ilgili sürüm başlığı altına taşınır (ör. `## [0.1.0] - YYYY-AA-GG`).
