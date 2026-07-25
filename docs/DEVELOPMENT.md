# Geliştirme Kılavuzu

Bu doküman RowingClub backend'inde lokal geliştirme ortamının nasıl kurulacağını, testlerin nasıl çalıştırılacağını ve yeni bir özelliğin hangi adımlarla ekleneceğini açıklar.

## 1. Gerekli SDK ve Araçlar

| Araç | Sürüm / Not |
|---|---|
| .NET SDK | .NET 10 (güncel LTS) |
| Docker Desktop | Docker Compose v2 destekli |
| PostgreSQL | Platformun tek veri deposu; lokal geliştirmede `.env.developer`'daki `POSTGRES_*` değişkenleriyle işaret edilen bir sunucuya (uzak dev sunucusu veya lokal kurulum) bağlanılır — Testcontainers/Docker Compose gerekmez, tek node yeterlidir (replica set gibi bir gereklilik yoktur) |
| Redis | Docker Compose üzerinden çalıştırılır |
| IDE | Visual Studio 2022+ veya JetBrains Rider (nullable reference types ve `TreatWarningsAsErrors` desteği açık olmalı) |

Çözüm dosyası: `RowingClub.sln` (repo kökü). Tüm projeler `net10.0` hedefler, `Nullable` ve `ImplicitUsings` etkindir.

```powershell
dotnet --version   # 10.x olmalı
dotnet restore RowingClub.sln
dotnet build RowingClub.sln
```

## 2. Environment Değişkenleri

Ana geliştirme talimatının 10. bölümüne göre secret'lar `.env.developer` / `.env.product` dosyalarında tutulur; bu dosyalar **Git'e eklenmez** (`.gitignore` içinde tanımlıdır). Repository'de yalnızca `.env.example` bulunmalıdır. Lokal geliştirme için repo kökünde `.env.developer` adıyla bir kopya oluşturup değerleri doldurun:

```dotenv
APP_ENV=Development
POSTGRES_HOST=
POSTGRES_PORT=5432
POSTGRES_DATABASE_NAME=rowingclub
POSTGRES_USERNAME=
POSTGRES_PASSWORD=
REDIS_CONNECTION_STRING=
JWT_ISSUER=
JWT_AUDIENCE=
JWT_SIGNING_PRIVATE_KEY=
JWT_SIGNING_PUBLIC_KEY=
FIELD_ENCRYPTION_KEY_CURRENT=
FIELD_ENCRYPTION_KEY_PREVIOUS=
FIELD_ENCRYPTION_KEY_VERSION=
SMTP_HOST=
SMTP_USERNAME=
SMTP_PASSWORD=
OTEL_EXPORTER_OTLP_ENDPOINT=
GRAFANA_ADMIN_PASSWORD=
```

Kurallar:

- `.env.product` hiçbir koşulda repository içinde saklanmaz; production secret'ları CI/CD secret store, Docker/Kubernetes secrets, Vault veya cloud secret manager üzerinden enjekte edilir.
- Uygulama, zorunlu bir secret eksikse **fail-fast** davranmalıdır (başlatmayı reddetmelidir).
- Secret değerleri hiçbir koşulda loglanmaz.
- JWT imzalama RS256 veya ES256 ile yapılır; `kid` claim'i ile key rotation desteklenir.
- `POSTGRES_HOST`/`POSTGRES_PORT`/`POSTGRES_DATABASE_NAME`/`POSTGRES_USERNAME`/`POSTGRES_PASSWORD`, `EnvironmentConfigurationExtensions` tarafından `Postgres:*` config anahtarlarına map'lenir; gerçek Npgsql bağlantı stringi bu parçalardan `PostgresOptions.ConnectionString` içinde inşa edilir. PostgreSQL tek node üzerinde bile gerçek ACID transaction desteği sağladığından, MongoDB'deki gibi bir `replicaSet=rs0` gerekliliği **yoktur** (bkz. [ARCHITECTURE.md §9](./ARCHITECTURE.md#9-postgresql-ve-ef-core-veri-modeli)).

Detaylı açıklamalar için bkz. [SECURITY.md](./SECURITY.md#secret-yönetimi).

## 3. Docker Compose ile Ayağa Kaldırma

Destek servisleri (`api`, `redis`, `prometheus`, `grafana`) `deploy/docker/docker-compose.yml` altında tanımlıdır. PostgreSQL, `.env.developer`'daki `POSTGRES_*` değişkenleriyle işaret edilen bir sunucu üzerinden kullanılır — tek node yeterlidir, MongoDB'deki gibi bir replica set kurulumuna gerek yoktur. Genel kullanım şekli:

```powershell
docker compose -f deploy/docker/docker-compose.yml up -d
docker compose -f deploy/docker/docker-compose.yml logs -f api
docker compose -f deploy/docker/docker-compose.yml down
```

Servis listesi ve amaçları için bkz. [DEPLOYMENT.md](./DEPLOYMENT.md).

## 4. Migration Çalıştırma

Migration'lar **EF Core Migrations** ile yönetilir; `IMongoMigration`/`MongoMigrationRunner`/`MongoMigrationHostedService` sistemi tamamen kaldırılmıştır. Migration dosyaları `src/BuildingBlocks/RowingClub.BuildingBlocks.Infrastructure/Postgres/Migrations/` altında tutulur (ilki `InitialCreate`) ve uygulama başlangıcında `EfMigrationHostedService`, API host'u istek almaya başlamadan önce `context.Database.MigrateAsync()` çağırarak henüz uygulanmamış migration'ları (EF'in kendi `__EFMigrationsHistory` tablosundan tespit ederek) otomatik uygular. Lokal geliştirmede `dotnet run` ile API'yi başlatmak yeterlidir — ayrı bir "migration çalıştır" adımı gerekmez.

Yeni bir migration eklemek için:

1. İlgili modülün Infrastructure katmanında entity mapping'ini bir `IEntityTypeConfiguration<T>` sınıfı olarak yazın/güncelleyin (modül assembly'si, DI kaydındaki `PersistenceAssemblyMarker` üzerinden zaten `RowingClubDbContext`'e bağlıdır — bkz. [ARCHITECTURE.md §9](./ARCHITECTURE.md#9-postgresql-ve-ef-core-veri-modeli)).
2. Model değişikliğinden EF Core'un otomatik olarak diff üretmesi için şu komutu çalıştırın:

   ```powershell
   dotnet ef migrations add <Ad> `
     --project src/BuildingBlocks/RowingClub.BuildingBlocks.Infrastructure/RowingClub.BuildingBlocks.Infrastructure.csproj `
     --startup-project src/RowingClub.Api/RowingClub.Api.csproj `
     --context RowingClub.BuildingBlocks.Infrastructure.Postgres.RowingClubDbContext
   ```

3. Üretilen migration dosyasını (`Up`/`Down` metotları) gözden geçirin — EF'in otomatik diff'i genelde doğrudur ama özellikle veri taşıyan (data migration) veya isim değişikliği içeren senaryolarda elle düzeltme gerekebilir.
4. Uygulamayı `dotnet run` ile başlatmak migration'ı otomatik uygular; manuel olarak uygulamak isterseniz `dotnet ef database update` (aynı `--project`/`--startup-project`/`--context` parametreleriyle) kullanılabilir.
5. **Zaten yayınlanmış (production'a gitmiş) bir migration'ı asla değiştirmeyin** — bir düzeltme gerekiyorsa yeni bir migration ekleyin (bkz. [DEPLOYMENT.md §6 Rollback](./DEPLOYMENT.md#6-rollback)).

Kurallar:

- Migration gerektiren bir domain değişikliği migration'sız merge edilemez (bkz. CI/CD kalite kapıları).
- Integration testleri (`PostgresIdentityFixture`) `.env.developer`'daki ile aynı uzak dev PostgreSQL sunucusuna bağlanır (Docker/Testcontainers **kullanılmaz**), ayrı bir `rowingclub_tests` veritabanını her test koşusunda drop+create edip gerçek EF Core migration'larıyla (`Database.MigrateAsync()`) migrate ederek doğrular — böylece test şeması hiçbir zaman production'dan sapmaz; ayrı bir "migration validation" CI adımına ihtiyaç yoktur, normal test koşusu yeterlidir.

## 5. Test Komutları

Beş test projesi bulunur, hepsi `RowingClub.sln` altında derlenir:

| Proje | Amaç |
|---|---|
| `tests/RowingClub.UnitTests` | Domain kuralları, handler, validator, authorization handler testleri |
| `tests/RowingClub.IntegrationTests` | PostgreSQL repository (`RowingClubDbContext`, EF Core migration'lar), outbox, tenant filter, encryption, refresh token rotation, idempotency, optimistic concurrency |
| `tests/RowingClub.FunctionalTests` | Gerçek HTTP request üzerinden authentication, role-policy, tenant isolation, validation, Problem Details, rate limiting, OData sınırları |
| `tests/RowingClub.ArchitectureTests` | NetArchTest ile katman ve modül bağımlılık kuralları |
| `tests/RowingClub.SecurityTests` | Token yok/yanlış rol/yanlış tenant/IDOR/tenant manipülasyonu güvenlik regresyon testleri |

Çalıştırma:

```powershell
dotnet test RowingClub.sln

# Tek proje
dotnet test tests/RowingClub.UnitTests/RowingClub.UnitTests.csproj
dotnet test tests/RowingClub.IntegrationTests/RowingClub.IntegrationTests.csproj
dotnet test tests/RowingClub.ArchitectureTests/RowingClub.ArchitectureTests.csproj

# Coverage ile
dotnet test RowingClub.sln --collect:"XPlat Code Coverage"
```

Integration testleri, PostgreSQL için Testcontainers/Docker **kullanmaz** — `PostgresIdentityFixture`, `.env.developer`'daki ile aynı uzak dev PostgreSQL sunucusuna bağlanır ve ayrı bir `rowingclub_tests` veritabanını her koşuda drop+create edip gerçek EF Core migration'larıyla migrate eder (bkz. §4). Eski `MongoIdentityFixture`'ın Testcontainers.MongoDb bağımlılığı bu geçişle birlikte tamamen kaldırıldı.

## 6. Seed Data Yaklaşımı

- Lokal ve test ortamları için idempotent seed script'leri kullanılır (uygulama başlangıcında veya ayrı bir CLI komutuyla çalıştırılır).
- Seed veri, gerçekçi ama üretim dışı örnek kulüp/kullanıcı/ders verisi içerir; hiçbir gerçek kişisel veri barındırmaz.
- Şifre/hash gibi hassas alanlar seed'de de gerçek hash algoritmasıyla (Argon2id/PBKDF2) üretilir, düz metin saklanmaz.
- Seed data yalnızca `Development` ve `Test` ortamlarında çalışır; production'da tetiklenmez (`APP_ENV` kontrolü ile korunur).

## 7. Kod Standartları (Talimat Bölüm 24 — SOLID ve Kod Kalitesi)

- SOLID prensipleri (Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion).
- Küçük ve anlamlı sınıflar; magic string kullanılmaz, merkezi permission sabitleri kullanılır.
- Tüm asenkron metotlarda `CancellationToken` parametresi ve `Async` suffix zorunludur.
- `Nullable` referans tipleri açık, `TreatWarningsAsErrors` etkin (warning as error).
- Anlamlı domain isimleri; gereksiz generic repository kullanılmaz; aggregate sınırlarına uyulur.
- Tarih alanlarında her zaman `DateTime.UtcNow` (veya `TimeProvider`) kullanılır, lokal saat kullanılmaz.
- Para alanları `decimal` tipinde ve currency bilgisiyle birlikte bir value object üzerinden taşınır.
- Concurrency-kritik entity'lerde concurrency token (`RowVersion`/optimistic concurrency) kullanılır.

## 8. Yeni Özellik Geliştirme Adımları

Ana geliştirme talimatının 27. bölümünde tanımlanan 16 adımlık süreç bu projede birebir uygulanır:

1. Mevcut solution ve dokümanları incele (`docs/` dizini, ilgili ADR'ler).
2. Etkilenen bounded context'i (modülü) belirle.
3. İş kurallarını ve güvenlik sınırlarını yaz.
4. `docs/API_ENDPOINTS.md` içinde eklenecek/değişecek endpoint taslağını oluştur.
5. `docs/AUTHORIZATION_MATRIX.md` içinde rol ve policy taslağını oluştur.
6. Domain modelini (entity, aggregate, value object, domain event, domain service) geliştir.
7. Application katmanında command/query, handler ve validator'ları geliştir.
8. Infrastructure implementasyonunu (PostgreSQL/EF Core repository, entity konfigürasyonu, gerekiyorsa yeni bir EF Core migration'ı, outbox, adaptörler) geliştir.
9. Minimal API endpoint'ini `RowingClub.Api` altında route group olarak ekle.
10. Authorization ve tenant güvenliğini uygula (policy + handler seviyesi kaynak kontrolü + `ICurrentTenant`/`ICurrentUser`).
11. Unit, integration, functional ve security testlerini yaz.
12. OpenAPI çıktısını güncelle.
13. Tüm ilgili dokümantasyonu güncelle (`API_ENDPOINTS.md`, `AUTHORIZATION_MATRIX.md`, etkilenen mimari dokümanlar, `CHANGELOG.md`).
14. Build ve testleri çalıştır.
15. Hataları düzelt.
16. Yapılan değişiklikleri ve riskleri, talimat 27. bölümdeki standart "Feature Özeti" formatında raporla.

Bir özellik yalnızca [DEFINITION_OF_DONE — bkz. ARCHITECTURE.md notları ve ana talimat Bölüm 26] karşılandığında tamamlanmış sayılır: kod derleniyor, tüm testler geçiyor, tenant/authorization kontrolleri uygulanmış, dokümantasyon güncel, hassas veri sızıntısı yok.
