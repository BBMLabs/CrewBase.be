---
name: migration-manager
description: EF Core migration üretme, doğrulama ve uygulama işlerinde kullanılır. Katalog ve tenant olmak üzere iki ayrı migration zinciri vardır.
---

# Migration Manager

## İki zincir

| | Katalog | Tenant |
|---|---|---|
| Context | `RowingClubDbContext` | `TenantDbContext` |
| Proje (-p) | `src/BuildingBlocks/RowingClub.BuildingBlocks.Infrastructure` | `src/Modules/Scheduling/RowingClub.Scheduling.Infrastructure` |
| Çıkış (-o) | `Postgres/Migrations` | `Persistence/Migrations` |
| Uygulanma | açılışta `EfMigrationHostedService` | açılışta `TenantMigrationHostedService` + kayıt anında provisioner (tüm tenant DB'lere tek tek) |

## Komut şablonu

```bash
dotnet ef migrations add <Ad> -p <proje> -s src/RowingClub.Api -o <çıkış> --context <Context>
```

Startup projesi her zaman `src/RowingClub.Api`. TenantDbContext design-time'da
`TenantDbContextDesignTimeFactory` + `EF.IsDesignTime` korumasıyla ayağa kalkar - bunları bozma.

## Kurallar

1. Migration üretince dosyayı OKU: beklenmedik drop/alter var mı?
2. Dolu tabloya NOT NULL kolon eklerken default + backfill SQL yaz
   (`AddCompanyTenantDatabase` migration'ındaki `migrationBuilder.Sql` örneği).
3. Unique index eklemeden önce mevcut veride çakışma olasılığını backfill ile çöz.
4. Tenant zincirinde `EnsureCreated` kullanma; şema yalnızca migration'la ilerler.
5. Migration'ı elle düzenlediysen Designer/snapshot ile tutarlılığı `dotnet build` +
   `scripts/validate-migration.sh` ile doğrula.
6. Geri alma: `dotnet ef migrations remove` (yalnızca henüz hiçbir DB'ye uygulanmadıysa).
   Uygulanmışsa yeni bir düzeltme migration'ı yaz.
