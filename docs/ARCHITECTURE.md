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
| **Infrastructure** | MongoDB repository implementasyonları (`IMongoUnitOfWork` üzerinden), Redis, Outbox/Inbox, JWT, şifreleme, e-posta/SMS/push adaptörleri, telemetry, background worker'lar | Application + Domain + `BuildingBlocks.Infrastructure`'a bağımlı. |
| **Api** | Minimal API route group'ları, authentication, authorization policy bağlantıları, exception middleware, correlation id, rate limiting, request logging, OpenAPI, OData endpoint'leri, health endpoint'leri, API versioning | Yalnızca `RowingClub.Bootstrapper`'a bağımlı; hiçbir modülün Infrastructure katmanına doğrudan dokunmaz. |
| **Bootstrapper** | Tüm modüllerin Application + Infrastructure kayıtlarının DI kompozisyonu | Tüm modüllerin Application ve Infrastructure projelerine bağımlı. |

Bu kurallar `tests/RowingClub.ArchitectureTests` içinde NetArchTest ile otomatik doğrulanır: Domain katmanları Infrastructure'a, `MediatR`'a veya `FluentValidation`'a bağımlı olamaz; Application katmanları `MongoDB.Driver`'a bağımlı olamaz; bir modülün Infrastructure'ı başka bir modülün Infrastructure'ına veya `RowingClub.Api`/`RowingClub.Bootstrapper`'a doğrudan bağımlı olamaz. `IMongoDatabase`/koleksiyonlara `RowingClub.Api`'den doğrudan erişilmez (bkz. §9.9); handler dışından transaction yönetimi yapılamaz.

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
    → TransactionBehavior (Command'larda: `IUnitOfWork.SaveChangesAsync` → tek MongoDB multi-document transaction)
    → LoggingBehavior
    → IdempotencyBehavior (Idempotency-Key header'lı komutlarda)
    → PerformanceBehavior (yavaş handler tespiti)
      → Handler (Domain'i çağırır, repository'ler aracılığıyla persist eder)
    ← DTO / sonuç
  ← HTTP response (Problem Details veya başarı DTO'su)
```

Command'lar state değiştirir; repository'ler ilgili aggregate'i `IMongoUnitOfWork.Track()` ile izler ve handler sonunda tek bir `SaveChangesAsync` çağrısı, aggregate yazımı + outbox kaydını aynı MongoDB transaction'ında üretir (bkz. §8). Query'ler salt okunur DTO döner ve tenant filtreli repository/OData üzerinden çalışır.

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
9. **Tenant filtreli repository** — `MongoUnitOfWork.Track()` her aggregate'i izlemeye aldığı anda `ITenantOwned` ise `ICurrentTenant` ile karşılaştırır (uyuşmazlıkta `DomainException("tenant_mismatch", ...)`); bu, EF Core global query filter'ının Mongo-native karşılığıdır.
10. **Audit ve telemetry** — işlem audit log'a ve trace/metric'lere yazılır.

Ek kurallar: route içindeki `clubId`, header'daki `X-Club-Id` ve current tenant birbiriyle eşleşmelidir; repository metotları tenant filtresiz sorgu çalıştıramaz; `SaveChanges` sırasında tenant'a bağlı entity'lerin `ClubId` değeri doğrulanır; Platform Admin bypass işlemleri özel policy + audit kaydı gerektirir. Tenant izolasyonu `RowingClub.SecurityTests` içinde otomatik test edilir.

## 8. Outbox/Inbox Akışı

```
Aggregate → Domain Event üretir (RaiseDomainEvent, henüz persist edilmemiş)
  → Repository, aggregate'i IMongoUnitOfWork.Track() ile izlemeye alır (Add'te VE her load'da - bkz. §9.3)
    → Handler sonunda IUnitOfWork.SaveChangesAsync() TEK bir MongoDB multi-document transaction açar:
        1. İzlenen her aggregate, {_id, version} filtresiyle whole-document replace/upsert edilir
           (optimistic concurrency - bkz. §9.4); versiyon uyuşmazlığında ConcurrencyException (→ HTTP 409)
        2. İzlenen her aggregate'in DomainEvents'i AYNI transaction içinde outbox_messages koleksiyonuna yazılır
        3. Transaction commit edilir, aggregate'lerin ClearDomainEvents() çağrılır
      → (henüz yok) Bir background worker outbox_messages'ı batch olarak, lock/claim mekanizmasıyla okur
        → Mesaj yayınlanır; consumer inbox_messages koleksiyonuna (eventId, consumerName) unique index
          üzerinden idempotent şekilde işler
          → Başarısızlıkta retry + exponential backoff; kalıcı başarısızlıkta dead-letter
```

Tek veritabanı olduğu için aggregate yazımı ile outbox kaydı arasında **distributed transaction gerekmez** — ikisi de aynı MongoDB session/transaction'ı içindedir, bu yüzden bu adım artık atomik ve senkron. Eventual consistency yalnızca outbox'tan sonraki adımda (worker'ın mesajı okuyup yayınlaması) başlar; işlenmiş mesaj ID'lerinin (inbox) saklanmasıyla idempotent tüketim sağlanır. Outbox lag/retry metrikleri üretilir (bkz. [OBSERVABILITY.md](./OBSERVABILITY.md)).

## 9. MongoDB Veri Modeli

Platformun **tek** veri deposu MongoDB'dir (bkz. [adr/0002-mongodb-tek-veritabani-stratejisi.md](./adr/0002-mongodb-tek-veritabani-stratejisi.md)) — Oracle veya başka bir ilişkisel veritabanı kullanılmaz. Tek bir mantıksal veritabanı (`Mongo:DatabaseName`, varsayılan `rowingclub`) altında, her modül kendi koleksiyonlarını `<modül>_<aggregate>` adlandırma kuralıyla yazar (ör. `identity_users`, `identity_refresh_tokens`); paylaşılan koleksiyonlar `outbox_messages`, `inbox_messages`, `schema_migrations`'dır. Tam koleksiyon/indeks listesi için bkz. [DATA_MODEL.md](./DATA_MODEL.md).

1. **Replica set zorunlu.** `MongoUnitOfWork`'ün kullandığı multi-document transaction'lar yalnızca bir replica set'e karşı çalışır; `Mongo:ConnectionString` (`MONGODB_CONNECTION_STRING`) `replicaSet=rs0` (veya eşdeğeri) içermek zorundadır. `docker-compose.yml`'deki `mongodb` servisi `--replSet rs0` ile başlar ve healthcheck'i ilk ayağa kalkışta tek node'luk replica set'i idempotent şekilde initiate eder.
2. **MongoUnitOfWork / Track-on-read deseni.** Repository'ler koleksiyona doğrudan yazmaz; her `GetByIdAsync`/`GetByEmailAsync` gibi bir aggregate döndüren metot VE `Add`, sonucu `unitOfWork.Track(collection, entity)` ile izlemeye alır. MongoDB.Driver'ın EF Core benzeri bir change tracker'ı olmadığından, yüklendikten sonra mutate edilen bir aggregate, load anında track edilmemişse mutasyon `SaveChangesAsync`'te sessizce kaybolur.
3. **`SaveChangesAsync`.** Tek bir MongoDB session/transaction açar, izlenen her aggregate'i whole-document replace ile (id + beklenen `Version` filtresiyle) yazar, ardından tüm izlenen aggregate'lerin domain event'lerini aynı transaction'da `outbox_messages`'a yazar, sonra commit eder (bkz. §8).
4. **Optimistic concurrency.** Her `AggregateRoot<TId>`'nin `Version` (long) alanı vardır; domain katmanı asla dokunmaz, yalnızca persistence katmanı `IncrementVersion()` ile artırır. Yazma filtresi `{_id, version}` eşleşmesi arar; eşleşme yoksa `ConcurrencyException` fırlatılır (global exception handler → HTTP 409). Ayrı bir kilitleme mekanizması yoktur.
5. **Versiyonlu migration + index/şema yönetimi.** `IMongoMigration` (`Version`, `Name`, `ExecuteAsync`) implementasyonları DI ile (`IEnumerable<IMongoMigration>`) toplanır ve uygulama başlangıcında `MongoMigrationHostedService`/`MongoMigrationRunner` tarafından bir kez çalıştırılır; uygulanan versiyonlar `schema_migrations` koleksiyonunda tutulduğundan yeniden deploy'da atlanır. Global sıralamayı korumak için modül başına versiyon aralığı ayrılmıştır: BuildingBlocks 1-99, Identity 100-199, Clubs 200-299, Memberships 300-399, Scheduling 400-499, Packages 500-599, Notifications 600-699, Reporting 700-799. Her migration ayrıca ilgili indeksleri kurar ve koleksiyonu (yoksa) bir `$jsonSchema` validator'ı ile (yalnızca zorunlu alanlar, katı olmayan) oluşturur.
6. **Outbox/Inbox.** `OutboxMessage` artık tamamen Mongo-native'dir (EF konfigürasyonu yok), aggregate'i tetikleyen domain event ile aynı transaction'da yazılır. `InboxMessage` (`eventId`+`consumerName` üzerinde unique index), henüz kurulmamış olan ilk idempotent consumer'ın (Notifications modülü hâlâ scaffold) kullanacağı şekli tanımlar.
7. **Tenant filtreleme.** `MongoUnitOfWork.Track`, `ITenantOwned` aggregate'leri track anında `ICurrentTenant`'a karşı doğrular (uyuşmazlıkta `DomainException("tenant_mismatch", ...)`) — EF Core global query filter + SaveChanges interceptor'ının Mongo-native karşılığı. Identity'nin kendi aggregate'leri (`User`, `Credential`, `RefreshToken`, `UserSession`, `EmailVerificationToken`, `PasswordResetToken`) `ITenantOwned` implemente **etmez**; Identity platform seviyesindedir, kulübe bağlı değildir (bir kullanıcı birden çok kulübe üye olabilir).
8. **Modüller/koleksiyonlar arası transaction.** Tek veritabanı ve tek `IMongoUnitOfWork` sayesinde, birden fazla aggregate'i kapsayan bir handler (ör. randevu + kontenjan + paket hakkı, Scheduling/Packages implemente edildiğinde) birden fazla koleksiyondan aggregate'leri `Track()` edebilir; hepsi tek `SaveChangesAsync` çağrısında atomik olarak commit edilir, özel bir mekanizma gerekmez.
9. **Katman kuralı korunuyor.** `IMongoDatabase`/koleksiyonlara `RowingClub.Api`'den asla doğrudan dokunulmaz — yalnızca modül Application/Infrastructure katmanı, Bootstrapper üzerinden erişir (Api → yalnızca Bootstrapper kuralı değişmedi).
10. **Value object'ler nested subdocument olarak map'lenir.** MongoDB'nin otomatik class-mapping convention'ları altında (özel bir `HasConversion` eşdeğeri gerekmedi) — ör. `User.Email` (bir `string Value` saran `EmailAddress`) `{ "email": { "value": "..." } }` olarak serialize olur, bu yüzden unique index `email.value` yolu üzerine kurulur, `email` üzerine değil. Bu, boş bir varsayım değil; bir BSON round-trip scratch testiyle doğrulandı.

## 10. Diyagramlar

Sistem/bileşen/sekans diyagramları `docs/diagrams/` altında tutulur (mevcut durumda boş; modüller ilerledikçe eklenecektir).
