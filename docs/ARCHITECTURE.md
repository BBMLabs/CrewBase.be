# Mimari

## 1. Sistem Bağlamı

RowingClub; kürek kulüplerinin üyelerini, eğitmenlerini, derslerini, randevularını, ders paketlerini, katılım bilgilerini ve kulüp operasyonlarını yönettiği, çok kiracılı (`multi-tenant`) bir ASP.NET Core Minimal API backend'idir. Bir kullanıcı birden fazla kulübe üye olabilir ve her kulüpte farklı rol/yetkilere sahip olabilir; kulüpler platforma tenant olarak kayıt olur ve her kulübün verisi diğerlerinden kesin olarak izole edilir.

Mimari yaklaşım: **Modüler Monolith** + **Domain-Driven Design** + **Clean Architecture** + **CQRS** (MediatR üzerinden).

## 2. Çözüm Yapısı ve Mevcut Durum

```text
src/
  RowingClub.Api/                 # Minimal API, route group'lar, middleware
  RowingClub.Bootstrapper/        # Tüm modüllerin DI kompozisyonu, host wiring
  BuildingBlocks/                 # Modüller arası paylaşılan, tenant/domain'e özgü olmayan altyapı
    RowingClub.BuildingBlocks.Domain
    RowingClub.BuildingBlocks.Application
    RowingClub.BuildingBlocks.Infrastructure
    RowingClub.BuildingBlocks.Security
    RowingClub.BuildingBlocks.Observability
  Modules/
    Identity/    {Domain, Application, Infrastructure, Contracts}   → İMPLEMENTE EDİLİYOR
    Clubs/       {Domain, Application, Infrastructure, Contracts}   → planlandı (scaffold)
    Memberships/ {Domain, Application, Infrastructure, Contracts}   → planlandı (scaffold)
    Scheduling/  {Domain, Application, Infrastructure, Contracts}   → planlandı (scaffold)
    Packages/    {Domain, Application, Infrastructure, Contracts}   → planlandı (scaffold)
    Notifications/{Domain, Application, Infrastructure, Contracts}  → planlandı (scaffold)
    Reporting/   {Application, Infrastructure}                      → planlandı (scaffold)
tests/
  RowingClub.UnitTests / IntegrationTests / FunctionalTests / ArchitectureTests / SecurityTests
```

Modüller ileride bağımsız servislere ayrılabilecek şekilde, net sınırlar (`Contracts` projeleri hariç diğer modüllerin iç katmanlarına doğrudan referans verilmeyecek şekilde) tasarlanmıştır.

## 3. Katman Sorumlulukları

| Katman | İçerik | Bağımlılık kuralı |
|---|---|---|
| **Domain** | Entity, Aggregate Root, Value Object, Domain Event, Domain Service, repository interface'leri, Specification, domain exception, iş kuralları | Hiçbir dış bağımlılığı yok — yalnızca `BuildingBlocks.Domain`'e bağımlı olabilir. Infrastructure veya API'ye asla bağımlı olmaz. |
| **Application** | Command, Query, Handler (MediatR), DTO, FluentValidation validator, authorization requirement, mapping, transaction/validation/logging/idempotency/performance pipeline behavior'ları | Domain + kendi `Contracts` projesi + `BuildingBlocks.Domain`/`BuildingBlocks.Application`'a bağımlı. Infrastructure veya API'ye bağımlı olamaz. |
| **Infrastructure** | PostgreSQL/EF Core repository implementasyonları (`RowingClubDbContext` üzerinden), Redis, Outbox/Inbox, JWT, şifreleme, e-posta/SMS/push adaptörleri, telemetry, background worker'lar | Application + Domain + `BuildingBlocks.Infrastructure`'a bağımlı. |
| **Api** | Minimal API route group'ları, authentication, authorization policy bağlantıları, exception middleware, correlation id, rate limiting, request logging, OpenAPI, OData endpoint'leri, health endpoint'leri, API versioning | Yalnızca `RowingClub.Bootstrapper`'a bağımlı; hiçbir modülün Infrastructure katmanına doğrudan dokunmaz. |
| **Bootstrapper** | Tüm modüllerin Application + Infrastructure kayıtlarının DI kompozisyonu | Tüm modüllerin Application ve Infrastructure projelerine bağımlı. |

Bu kurallar `tests/RowingClub.ArchitectureTests` içinde NetArchTest ile otomatik doğrulanır: Domain katmanları Infrastructure'a, `MediatR`'a veya `FluentValidation`'a bağımlı olamaz; Application katmanları `Microsoft.EntityFrameworkCore`'a bağımlı olamaz; bir modülün Infrastructure'ı başka bir modülün Infrastructure'ına veya `RowingClub.Api`/`RowingClub.Bootstrapper`'a doğrudan bağımlı olamaz. `RowingClubDbContext`'e `RowingClub.Api`'den doğrudan erişilmez (bkz. §9); handler dışından transaction yönetimi yapılamaz.

Gerçek referans grafiği (mevcut `.csproj` dosyalarından): `Identity.Application → Identity.Domain + Identity.Contracts + BuildingBlocks.{Domain,Application}`; `Identity.Infrastructure → Identity.Application + Identity.Domain + BuildingBlocks.{Infrastructure,Application,Domain}`; `Bootstrapper → tüm modüllerin Application+Infrastructure'ı + BuildingBlocks'un tamamı`; `Api → yalnızca Bootstrapper` (Program.cs şu an minimal iskelet halinde, modül wiring'i Identity çalışması tamamlandıkça Bootstrapper üzerinden bağlanacak).

## 4. Bounded Context'ler

### 4.1 Identity — **implemente ediliyor**

Kullanıcı kaydı, giriş/çıkış, e-posta doğrulama, şifre sıfırlama, access token, refresh token rotation, token family ve reuse detection, kullanıcı kilitleme, cihaz/oturum yönetimi, MFA altyapısı.

Ana nesneler: `User`, `Credential`, `RefreshToken`, `UserSession`, `EmailVerificationToken`, `PasswordResetToken`.

### 4.2 Clubs — planlandı

Kulüp oluşturma, kulüp profili, kulüp durumu, tenant ayarları, şubeler/lokasyonlar, çalışma saatleri, kulüp aboneliği.

Ana nesneler: `Club`, `ClubBranch`, `ClubSettings`, `ClubSubscription`.

### 4.3 Memberships — planlandı

Kullanıcı-kulüp ilişkisi, kulüp içi roller, üye daveti, üyelik durumu, eğitmen ilişkisi, üyelik başlangıç/bitiş tarihi.

Ana nesneler: `ClubMembership`, `MembershipInvitation`, `ClubRoleAssignment`.

### 4.4 Scheduling — planlandı

Ders tipi, ders seansı, randevu, kontenjan, bekleme listesi, katılım, no-show, ders iptali, tekrarlayan ders, eğitmen uygunluğu, tekne/ekipman rezervasyonu.

Ana nesneler: `LessonType`, `LessonSession`, `Appointment`, `Attendance`, `WaitlistEntry`, `InstructorAvailability`, `EquipmentReservation`.

### 4.5 Packages — planlandı

Ders paketi tanımı, üyeye paket atama, paket satışı, paket başlangıç/bitiş tarihi, kalan kullanım hakkı, paket dondurma, kullanım hareketi, iade/düzeltme.

Ana nesneler: `LessonPackageDefinition`, `MemberPackage`, `PackageUsageTransaction`.

### 4.6 Notifications — planlandı

Push notification, e-posta, SMS adaptörü, ders hatırlatması, iptal bildirimi, davet bildirimi, outbox event tüketimi.

### 4.7 Reporting — planlandı

Üye devamlılığı, ders doluluk oranı, paket satış/kullanım raporu, eğitmen performansı, kulüp bazlı operasyon raporları, kontrollü OData sorguları.

## 5. DDD Yaklaşımı

- Her modülün Domain katmanı kendi aggregate root'larını ve iş kurallarını kapsüller; aggregate sınırları dışına doğrudan erişim yoktur.
- Value object'ler (ör. para, currency, telefon, e-posta) immutable ve kendi kendini doğrulayan tiplerdir.
- Domain event'ler aggregate tarafından üretilir ve outbox mekanizması ile aynı transaction içinde kalıcı hale getirilir (bkz. §7).
- Modüller arası iletişim, doğrudan başka bir modülün Domain/Infrastructure'ına referans vererek değil, **Contracts** projeleri (paylaşılan DTO/event sözleşmeleri) ve/veya entegrasyon event'leri üzerinden yapılır.

## 6. CQRS Akışı (MediatR)

```
Minimal API endpoint
  → MediatR.Send(Command veya Query)
    → ValidationBehavior (FluentValidation)
    → AuthorizationBehavior (application-level kontrol)
    → TransactionBehavior (Command'larda: `IUnitOfWork.SaveChangesAsync` → tek PostgreSQL transaction, EF Core'un kendi `SaveChanges` transaction'ı)
    → LoggingBehavior
    → IdempotencyBehavior (Idempotency-Key header'lı komutlarda)
    → PerformanceBehavior (yavaş handler tespiti)
      → Handler (Domain'i çağırır, repository'ler aracılığıyla persist eder)
    ← DTO / sonuç
  ← HTTP response (Problem Details veya başarı DTO'su)
```

Command'lar state değiştirir; repository'ler ilgili aggregate'i `RowingClubDbContext` üzerinden yükler/ekler — EF Core'un ChangeTracker'ı, entity bir kez context üzerinden yüklendiğinde onu otomatik olarak izlemeye alır, ayrı bir "Track()" çağrısına gerek yoktur. Handler sonunda tek bir `SaveChangesAsync` çağrısı, aggregate yazımı + outbox kaydını aynı PostgreSQL transaction'ında üretir (bkz. §8). Query'ler salt okunur DTO döner ve tenant filtreli repository/OData üzerinden çalışır.

## 7. Tenant Çözümleme Akışı

Aktif tenant, `X-Club-Id` request header'ı üzerinden alınır ve **istemciden geldiği için güvenilir kabul edilmez** — sunucu tarafında kullanıcının aktif üyelikleriyle doğrulanır. `ICurrentTenant` ve `ICurrentUser` abstraction'ları bu bilgiyi request scope'unda taşır.

Request işleme sırası (talimat §7):

1. **Authentication** — JWT doğrulanır.
2. **Tenant header doğrulama** — `X-Club-Id` formatı ve varlığı kontrol edilir.
3. **Aktif üyelik doğrulama** — kullanıcının bu kulüpte aktif üyeliği olup olmadığı sunucu tarafında sorgulanır.
4. **Tenant context oluşturma** — `ICurrentTenant` doldurulur.
5. **Endpoint authorization** — route bazlı policy kontrolü.
6. **Application authorization** — handler/pipeline seviyesinde permission ve kaynak sahipliği kontrolü.
7. **Validation** — FluentValidation.
8. **Handler** — iş mantığı çalışır.
9. **Tenant filtreli repository** — `RowingClubDbContext.SaveChangesAsync`, değişen her `ITenantOwned` entity'yi `ICurrentTenant` ile karşılaştırır (uyuşmazlıkta `DomainException("tenant_mismatch", ...)`); bu kontrol yazma anında uygulanır. Henüz `ClubId` üzerinde global bir EF Core query filter'ı **yoktur** — okuma yolunda tenant filtresi repository sorgularının kendisi tarafından uygulanır.
10. **Audit ve telemetry** — işlem audit log'a ve trace/metric'lere yazılır.

Ek kurallar: route içindeki `clubId`, header'daki `X-Club-Id` ve current tenant birbiriyle eşleşmelidir; repository metotları tenant filtresiz sorgu çalıştıramaz; `SaveChanges` sırasında tenant'a bağlı entity'lerin `ClubId` değeri doğrulanır; Platform Admin bypass işlemleri özel policy + audit kaydı gerektirir. Tenant izolasyonu `RowingClub.SecurityTests` içinde otomatik test edilir.

## 8. Outbox/Inbox Akışı

```
Aggregate → Domain Event üretir (RaiseDomainEvent, henüz persist edilmemiş)
  → Repository, aggregate'i RowingClubDbContext üzerinden yükler/ekler - EF Core'un ChangeTracker'ı
    entity bir kez context üzerinden yüklendiğinde/eklendiğinde onu otomatik izlemeye alır
    → Handler sonunda IUnitOfWork.SaveChangesAsync() → RowingClubDbContext.SaveChangesAsync TEK bir
      EF Core SaveChanges transaction'ı içinde:
        1. Değişen her ITenantOwned entity, ICurrentTenant'a karşı doğrulanır (tenant_mismatch)
        2. İzlenen her aggregate'in DomainEvents'i outbox_messages'a (OutboxMessages DbSet) eklenir
        3. Değişen her AggregateRoot'un Version'ı artırılır (optimistic concurrency)
        4. base.SaveChangesAsync() çağrılır - EF, tüm bu değişiklikleri TEK bir SQL transaction'ında
           commit eder; UPDATE'ler otomatik olarak WHERE version = @original ekler, eşleşmezse
           DbUpdateConcurrencyException fırlatılır ve ConcurrencyException'a (→ HTTP 409) çevrilir
        5. Aggregate'lerin ClearDomainEvents() çağrılır
      → (henüz yok) Bir background worker outbox_messages'ı batch olarak, lock/claim mekanizmasıyla okur
        → Mesaj yayınlanır; consumer inbox_messages tablosuna (EventId, ConsumerName) composite primary
          key üzerinden idempotent şekilde işler
          → Başarısızlıkta retry + exponential backoff; kalıcı başarısızlıkta dead-letter
```

Tek veritabanı olduğu için aggregate yazımı ile outbox kaydı arasında **distributed transaction gerekmez** — ikisi de aynı EF Core `SaveChanges` çağrısının açtığı tek PostgreSQL transaction'ı içindedir, bu yüzden bu adım atomik ve senkrondur (ayrıca artık açıkça bir BEGIN/COMMIT yönetmeye de gerek yoktur — EF bunu kendi SaveChanges'i içinde yapar). Eventual consistency yalnızca outbox'tan sonraki adımda (worker'ın mesajı okuyup yayınlaması) başlar; işlenmiş mesaj ID'lerinin (inbox) saklanmasıyla idempotent tüketim sağlanır. Outbox lag/retry metrikleri üretilir (bkz. [OBSERVABILITY.md](./OBSERVABILITY.md)).

## 9. PostgreSQL ve EF Core Veri Modeli

Platformun **tek** veri deposu PostgreSQL'dir, EF Core (`Npgsql.EntityFrameworkCore.PostgreSQL`) üzerinden erişilir (bkz. [adr/0004-postgresql-tek-veritabani-stratejisi.md](./adr/0004-postgresql-tek-veritabani-stratejisi.md)) — MongoDB veya başka bir doküman veritabanı kullanılmaz. Tek bir mantıksal veritabanı (`Postgres:DatabaseName`, varsayılan `rowingclub`) altında, her modül kendi tablolarını `<modül>_<aggregate>` adlandırma kuralıyla yazar (ör. `identity_users`, `identity_refresh_tokens`); paylaşılan tablolar `outbox_messages`, `inbox_messages`'dır. Tam tablo/indeks listesi için bkz. [DATA_MODEL.md](./DATA_MODEL.md).

Tüm modüllerin entity mapping'lerini tek bir yerde toplayan sınıf `RowingClubDbContext`'tir (`src/BuildingBlocks/RowingClub.BuildingBlocks.Infrastructure/Postgres/RowingClubDbContext.cs`):

1. **Tek paylaşılan `DbContext`.** Modüllerin ayrı ayrı `DbContext`'i yoktur; hepsi `RowingClubDbContext`'i paylaşır. Bir modülün entity mapping'leri (`IEntityTypeConfiguration<T>` implementasyonları), o modülün Infrastructure DI kaydının sunduğu bir `PersistenceAssemblyMarker(Assembly)` ile temsil edilir (eski `IMongoMigration`'ların DI ile toplanma şekline benzer); `RowingClubDbContext.OnModelCreating`, `IEnumerable<PersistenceAssemblyMarker>` üzerinden her marker için `modelBuilder.ApplyConfigurationsFromAssembly(marker.Assembly)` çağırır. Bu sayede `RowingClub.BuildingBlocks.Infrastructure` hiçbir modüle doğrudan referans vermez.
2. **Repository'ler ve otomatik change tracking.** Repository'ler (ör. `UserRepository`, `RefreshTokenRepository` — `Mongo` önekleri düşürüldü, artık tek bir persistence teknolojisi olduğu için) entity'leri `RowingClubDbContext` üzerinden yükler/ekler. EF Core'un ChangeTracker'ı, bir entity context üzerinden bir kez yüklendiğinde onu otomatik olarak izler; eski Mongo tasarımındaki gibi her `GetByIdAsync`/`Add`'te ayrı bir "Track()" çağrısına gerek yoktur — bu, eski manuel track-on-read deseninin ortadan kalkmasıyla gelen gerçek bir basitleştirmedir.
3. **`SaveChangesAsync`.** `RowingClubDbContext.SaveChangesAsync` override'ı, `base.SaveChangesAsync()`'i çağırmadan önce sırasıyla: (a) tenant sahiplik doğrulaması yapar, (b) izlenen aggregate'lerin domain event'lerini `OutboxMessages`'a ekler, (c) değişen her `AggregateRoot<Guid>`'in `Version`'ını artırır. Hepsi EF'in kendi tek `SaveChanges` transaction'ı içinde gerçekleşir; ayrıca açık bir BEGIN/COMMIT yönetmeye gerek yoktur (bkz. §8).
4. **Optimistic concurrency.** Her `AggregateRoot<TId>`'nin `Version` (long) alanı, EF Core tarafında `IsConcurrencyToken()` ile işaretlenmiştir; EF, UPDATE sorgularına otomatik olarak `WHERE version = @original` ekler. Eşleşme yoksa EF `DbUpdateConcurrencyException` fırlatır; `RowingClubDbContext.SaveChangesAsync` bunu yakalayıp aynı `ConcurrencyException(entityName, id)` domain hatasına çevirir (global exception handler → HTTP 409) — davranış eski Mongo tasarımıyla aynıdır, yalnızca mekanizma EF'in kendi concurrency token desteğidir.
5. **EF Core Migrations.** `IMongoMigration`/`MongoMigrationRunner`/`MongoMigrationHostedService` sisteminin yerini tamamen EF Core Migrations alır. Migration'lar `src/BuildingBlocks/RowingClub.BuildingBlocks.Infrastructure/Postgres/Migrations/` altında tutulur (ilki `InitialCreate`) ve uygulama başlangıcında `EfMigrationHostedService`'in çağırdığı `context.Database.MigrateAsync()` ile otomatik uygulanır. Uygulanan migration'lar EF'in kendi `__EFMigrationsHistory` tablosunda, dosya sırasına göre izlenir — artık modül başına ayrılmış manuel bir versiyon aralığı şeması yoktur. Yeni migration eklemek için bkz. [DATA_MODEL.md — EF Core Migrations](./DATA_MODEL.md#ef-core-migrations).
6. **Outbox/Inbox.** `OutboxMessage` ve `InboxMessage` şekli (sütunlar, `InboxMessage`'ın `(EventId, ConsumerName)` composite primary key'i) eski Mongo tasarımından birebir taşındı; artık `OutboxMessageConfiguration`/`InboxMessageConfiguration` ile EF Core tarafında konfigüre edilirler, aggregate'i tetikleyen domain event ile aynı `SaveChanges` transaction'ında yazılırlar.
7. **Tenant filtreleme.** `RowingClubDbContext.SaveChangesAsync`, `ChangeTracker.Entries<ITenantOwned>()` üzerinden değişen (Added/Modified) her entity'yi `ICurrentTenant`'a karşı doğrular (uyuşmazlıkta `DomainException("tenant_mismatch", ...)`) — bu, **yazma anında** uygulanan bir kontroldür. Şu an `ClubId` üzerinde global bir EF Core query filter'ı **yoktur**; okuma yolunda tenant filtresi repository sorgularının kendisi tarafından uygulanır. Identity'nin kendi aggregate'leri (`User`, `Credential`, `RefreshToken`, `UserSession`, `EmailVerificationToken`, `PasswordResetToken`) `ITenantOwned` implemente **etmez**; Identity platform seviyesindedir, kulübe bağlı değildir (bir kullanıcı birden çok kulübe üye olabilir).
8. **Modüller/tablolar arası transaction.** Tek veritabanı ve tek `RowingClubDbContext` sayesinde, birden fazla aggregate'i kapsayan bir handler (ör. randevu + kontenjan + paket hakkı, Scheduling/Packages implemente edildiğinde) birden fazla tablodan aggregate'leri yükleyip değiştirebilir; hepsi tek `SaveChangesAsync` çağrısında atomik olarak commit edilir, özel bir mekanizma gerekmez.
9. **Katman kuralı korunuyor.** `RowingClubDbContext`'e `RowingClub.Api`'den asla doğrudan dokunulmaz — yalnızca modül Application/Infrastructure katmanı, Bootstrapper üzerinden erişir (Api → yalnızca Bootstrapper kuralı değişmedi).
10. **Value object'ler ilişkisel sütunlara map'lenir.** EF Core `HasConversion`/`OwnsOne` ile — ör. `User.Email` (bir `string Value` saran `EmailAddress`) tek bir `Email` (text) sütununa map'lenir, unique index de bu sütun üzerine kurulur. Enum'lar (ör. `User.Status`) `HasConversion<string>()` ile text sütun olarak saklanır (eski `EnumRepresentationConvention(BsonType.String)`'in doğrudan karşılığı).
11. **Replica set / node zorunluluğu yok.** PostgreSQL tek node üzerinde bile gerçek ACID transaction desteği sağladığından, eski Mongo tasarımındaki "multi-document transaction yalnızca bir replica set'e karşı çalışır" kısıtı ortadan kalkmıştır; bağlantı stringinde `replicaSet=rs0` gibi bir gereklilik yoktur.

## 10. Diyagramlar

Sistem/bileşen/sekans diyagramları `docs/diagrams/` altında tutulur (mevcut durumda boş; modüller ilerledikçe eklenecektir).
