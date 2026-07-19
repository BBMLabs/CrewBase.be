# Geliştirme Kılavuzu

Bu doküman RowingClub backend'inde lokal geliştirme ortamının nasıl kurulacağını, testlerin nasıl çalıştırılacağını ve yeni bir özelliğin hangi adımlarla ekleneceğini açıklar.

## 1. Gerekli SDK ve Araçlar

| Araç | Sürüm / Not |
|---|---|
| .NET SDK | .NET 10 (güncel LTS) |
| Docker Desktop | Docker Compose v2 destekli |
| MongoDB | Platformun tek veri deposu; Docker Compose üzerinden çalıştırılır (lokal kurulum gerekmez), `--replSet rs0` ile |
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
MONGODB_CONNECTION_STRING=
MONGODB_DATABASE_NAME=rowingclub
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
- `MONGODB_CONNECTION_STRING` **`replicaSet=rs0`** (veya eşdeğeri) içermek zorundadır — `MongoUnitOfWork` her `SaveChangesAsync` çağrısında multi-document transaction açar, standalone bir `mongod` bunu desteklemez (bkz. [ARCHITECTURE.md §9.1](./ARCHITECTURE.md#9-mongodb-veri-modeli)). Örnek: `mongodb://mongodb:27017/rowingclub?replicaSet=rs0`.

Detaylı açıklamalar için bkz. [SECURITY.md](./SECURITY.md#secret-yönetimi).

## 3. Docker Compose ile Ayağa Kaldırma

Destek servisleri (`api`, `mongodb`, `redis`, `prometheus`, `grafana`) `deploy/docker/docker-compose.yml` altında tanımlıdır. `mongodb` servisi `--replSet rs0` ile başlar; healthcheck'i ilk ayağa kalkışta tek node'luk replica set'i idempotent şekilde initiate eder, ayrıca bir kurulum adımı gerekmez. Genel kullanım şekli:

```powershell
docker compose -f deploy/docker/docker-compose.yml up -d
docker compose -f deploy/docker/docker-compose.yml logs -f api
docker compose -f deploy/docker/docker-compose.yml down
```

Servis listesi ve amaçları için bkz. [DEPLOYMENT.md](./DEPLOYMENT.md).

## 4. Migration Çalıştırma

Manuel bir migration komutu **yoktur** — EF Core kullanılmıyor. Migration'lar `IMongoMigration` implementasyonlarıdır; DI'a kayıtlıdırlar ve `MongoMigrationHostedService`, API host'u başlarken (istek almadan önce) hepsini `Version`'a göre sırayla, henüz uygulanmamış olanları `schema_migrations` koleksiyonundan tespit ederek otomatik çalıştırır. Lokal geliştirmede `dotnet run` veya `docker compose up` ile API'yi başlatmak yeterlidir — ayrı bir "migration çalıştır" adımı gerekmez.

Yeni bir migration eklemek için:

1. İlgili modülün `Infrastructure/Persistence` klasöründe `IMongoMigration`'ı implemente eden bir sınıf yazın (`Version`, `Name`, `ExecuteAsync(IMongoDatabase, CancellationToken)`).
2. `Version` için modülünüze ayrılmış aralıktaki bir sonraki numarayı kullanın (bkz. [DATA_MODEL.md — Migration Versiyon Aralıkları](./DATA_MODEL.md#migration-versiyon-aralıkları); örn. Identity 100-199).
3. `ExecuteAsync` içinde koleksiyonu (yoksa) bir `$jsonSchema` validator'ıyla oluşturun ve gerekli indeksleri kurun — idempotent yazın (`CreateCollectionAsync` öncesi `ListCollectionNamesAsync` ile varlık kontrolü, bkz. `IdentityCollectionsMigration` örneği).
4. Yeni sınıfı modülün DI kayıtlarına `services.AddSingleton<IMongoMigration, YeniMigrasyon>()` olarak ekleyin.
5. **Zaten yayınlanmış bir migration'ı asla değiştirmeyin** — bir düzeltme gerekiyorsa yeni bir versiyon numarasıyla ayrı bir migration ekleyin (bkz. [DEPLOYMENT.md §6 Rollback](./DEPLOYMENT.md#6-rollback) — fix-forward yaklaşımı).

Kurallar:

- Migration gerektiren bir domain değişikliği migration'sız merge edilemez (bkz. CI/CD kalite kapıları).
- Integration testleri (`MongoIdentityFixture` gibi) migration'ları Testcontainers ile açılan geçici bir MongoDB replica set'ine karşı gerçekten çalıştırarak doğrular — ayrı bir "migration validation" CI adımına ihtiyaç yoktur, normal test koşusu yeterlidir.

## 5. Test Komutları

Beş test projesi bulunur, hepsi `RowingClub.sln` altında derlenir:

| Proje | Amaç |
|---|---|
| `tests/RowingClub.UnitTests` | Domain kuralları, handler, validator, authorization handler testleri |
| `tests/RowingClub.IntegrationTests` | MongoDB repository (`IMongoUnitOfWork`, migration'lar), outbox, tenant filter, encryption, refresh token rotation, idempotency, optimistic concurrency |
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

Integration testleri Testcontainers ile geçici MongoDB (Testcontainers'ın `MongoDbBuilder`'ı, tek-node replica set önceden initiate edilmiş halde) ve Redis konteynerleri açar; Docker Desktop'ın çalışıyor olması gerekir.

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
8. Infrastructure implementasyonunu (Mongo repository, gerekiyorsa yeni `IMongoMigration`, outbox, adaptörler) geliştir.
9. Minimal API endpoint'ini `RowingClub.Api` altında route group olarak ekle.
10. Authorization ve tenant güvenliğini uygula (policy + handler seviyesi kaynak kontrolü + `ICurrentTenant`/`ICurrentUser`).
11. Unit, integration, functional ve security testlerini yaz.
12. OpenAPI çıktısını güncelle.
13. Tüm ilgili dokümantasyonu güncelle (`API_ENDPOINTS.md`, `AUTHORIZATION_MATRIX.md`, etkilenen mimari dokümanlar, `CHANGELOG.md`).
14. Build ve testleri çalıştır.
15. Hataları düzelt.
16. Yapılan değişiklikleri ve riskleri, talimat 27. bölümdeki standart "Feature Özeti" formatında raporla.

Bir özellik yalnızca [DEFINITION_OF_DONE — bkz. ARCHITECTURE.md notları ve ana talimat Bölüm 26] karşılandığında tamamlanmış sayılır: kod derleniyor, tüm testler geçiyor, tenant/authorization kontrolleri uygulanmış, dokümantasyon güncel, hassas veri sızıntısı yok.
