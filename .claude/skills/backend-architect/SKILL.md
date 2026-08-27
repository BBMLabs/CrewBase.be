---
name: backend-architect
description: Bu repoda (RowingClub/CrewBase modüler monolit) yeni modül, katman veya büyük mimari değişiklik tasarlarken kullanılır. Katman kurallarını, modül sınırlarını ve database-per-tenant mimarisini uygular.
---

# Backend Architect

CrewBase.be, .NET 10 üzerinde modüler monolit bir ASP.NET Core API'sidir. Mimari kararlar
verirken bu skill'i ve `references/` dosyalarını izle.

## Temel yapı

- `src/BuildingBlocks/*` — Domain, Application, Infrastructure, Security, Observability:
  tüm modüllerin paylaştığı çekirdek. Modüllere ASLA bağımlı olamaz.
- `src/Modules/<Modül>/{Domain, Application, Infrastructure, Contracts}` — her modül kendi
  katmanlarına sahiptir. Bir modülün Infrastructure'ı başka modülün Infrastructure'ına bağımlı olamaz.
- `src/RowingClub.Bootstrapper` — modülleri tanıyan TEK yer; DI kayıtları burada birleşir.
- `src/RowingClub.Api` — kompozisyon kökü: endpoint'ler, tenant çözümü, hosted service'ler.
  Modüller arası "yapıştırma" (ör. Scheduling'in hatırlatma göndericisinin Identity SMTP'sine
  bağlanması) yalnızca burada yapılır.

## Veritabanı mimarisi

- **Katalog DB** (`RowingClubDbContext`): firmalar, kullanıcılar, kimlik verileri. EF migrations
  `BuildingBlocks.Infrastructure/Postgres/Migrations` altında.
- **Tenant DB'ler** (`TenantDbContext`, firma başına bir DB `tenant_<slug>`): randevu/üye/eğitmen/
  tekne/paket/ayar verileri. Migrations `Scheduling.Infrastructure/Persistence/Migrations` altında.
- Tenant çözümü: subdomain (public) veya JWT'deki CompanyId (panel) → `ITenantDatabase.Set(...)` →
  scoped `TenantDbContext` bağlantıyı buradan kurar. Yeni tenant-veri özelliği eklerken bu akışı boz ma.

## Karar kuralları

1. Yeni iş alanı → önce hangi modüle ait olduğuna karar ver; modül yoksa iskeleti mevcut
   Scheduling/Identity düzenini kopyalayarak kur.
2. Modüller arası ihtiyaç → önce MediatR query/command (Application seviyesi) veya
   BuildingBlocks'ta soyutlama; doğrudan proje referansı en son çare ve yalnızca Contracts'a.
3. Cross-cutting bir şeyse (şifreleme, idempotency, tenancy) BuildingBlocks'a koy.
4. Her mimari değişiklikten sonra `tests/RowingClub.ArchitectureTests` koşmalı ve geçmeli.

Ayrıntılar: `references/architecture-rules.md`, `references/module-patterns.md`.
