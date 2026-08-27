---
name: database-designer
description: Şema tasarımı ve değişikliği yaparken kullanılır. Katalog/tenant DB ayrımı, şifreli kolon kuralları, indeksleme ve EF map desenleri.
---

# Database Designer

İki ayrı şema dünyası vardır - önce hangisinde olduğunu belirle:

## Katalog DB (`rowingclub`, RowingClubDbContext)

Firmalar (`identity_companies`), kullanıcılar (`identity_users`), kimlik/oturum tabloları,
outbox/inbox. Migration: `BuildingBlocks.Infrastructure/Postgres/Migrations`.
Aggregate'ler `HasAggregateRootDefaults()` (Id + Version concurrency token) kullanır.

## Tenant DB'ler (firma başına `tenant_<slug>`, TenantDbContext)

`customers`, `appointments`, `training_sessions`, `boats`, `instructors`, `lesson_packages`,
`company_settings`. Migration: `Scheduling.Infrastructure/Persistence/Migrations`.
Firma kimliği kolonlara yazılmaz - izolasyon veritabanı seviyesindedir.

## Kurallar

1. PII (ad, telefon, e-posta, not, sağlık vs.) tenant DB'de şifreli `text` kolon
   (AES-GCM converter). Aranacaksa blind index kolonu ekle (`PhoneIndex` deseni) ve
   `SaveChangesAsync` override'ında doldur. Şifreli kolona SQL `WHERE/ORDER BY` yazılamaz.
2. Enum'lar string saklanır: `.HasConversion<string>().HasMaxLength(...)`.
3. Para `decimal` + `HasPrecision(12,2)`. Tarih `DateOnly`, saat `TimeOnly`,
   zaman damgası `DateTimeOffset` (`*AtUtc` isimlendirmesi).
4. Benzersizlik DB'de zorlanır (unique index); "iptal hariç" gibi kurallar filtered index:
   `.HasFilter("\"Status\" <> 'Cancelled'")`.
5. Şema değişikliği = migration; tenant tarafında `EnsureCreated` YASAK (migration zinciri bozulur).

İndeks seçimi için `references/indexing.md`, ayrıntılı kurallar için
`references/database-rules.md`.
