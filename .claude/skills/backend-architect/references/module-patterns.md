# Modül Desenleri

Yeni bir özellik eklerken mevcut modüllerdeki desenleri kopyala:

## Domain (ör. `Scheduling.Domain/Sessions/TrainingSession.cs`)

- Entity'ler: private ctor + statik factory (`Create`/`Register`/`Book`), davranış metodları
  içeride, iş kuralı ihlali `DomainException(code, mesaj)` fırlatır (kod snake_case İngilizce,
  mesaj Türkçe).
- Repository arayüzü aynı klasörde (`IXxxRepository`), yalnızca ihtiyaç duyulan sorgular.
- Değer kuralları entity'de sabit/metod olarak (`CompanySettings.EnsureBookable` gibi).

## Application (ör. `Scheduling.Application/Booking/`)

- Bir use-case = bir dosya kümesi: `XxxCommand.cs` (record + response record),
  `XxxCommandHandler.cs`, gerekiyorsa `XxxCommandValidator.cs` (FluentValidation).
- Yazan istekler `ICommand<T>` (BuildingBlocks.Application.Messaging), okuyanlar `IRequest<T>`.
- DTO'lar Application'da tanımlanır; Domain entity'si endpoint'e sızmaz.

## Infrastructure (ör. `Scheduling.Infrastructure/`)

- `DependencyInjection.AddXxxInfrastructure()` — tüm kayıtlar tek yerde.
- Repository'ler DbContext'i ctor'dan alır; `SaveChanges` çağırmaz (unit-of-work halleder;
  tenant tarafında handler `ISchedulingUnitOfWork` kaydeder).
- EF konfigürasyonu: katalog modülleri `IEntityTypeConfiguration<T>` + assembly marker ile,
  tenant tarafı `TenantDbContext.OnModelCreating` içinde.

## API endpoint (ör. `Api/Endpoints/CompanyPanelEndpoints.cs`)

- Minimal API `MapGroup` + `RequireAuthorization(Roles=...)`.
- Request record'ları endpoint dosyasının başında.
- Panel endpoint'i: `ICurrentUser.CompanyId` → `TenantResolver.ResolveByCompanyIdAsync` →
  yoksa 404; public endpoint: route'daki subdomain → `ResolveBySubdomainAsync`.
- Yanıtlar `ApiResponse<T>.Ok(...)` sarmalayıcısıyla.

## Bootstrapper

Yeni modül Application assembly'sini `AddRowingClubApplication(...)` çağrısına, Infrastructure
kaydını da ayrı satır olarak ekle.
