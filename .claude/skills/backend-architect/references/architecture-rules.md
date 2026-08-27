# Mimari Kurallar (NetArchTest ile zorlanır)

`tests/RowingClub.ArchitectureTests/LayeringTests.cs` bu kuralları koşar; ihlal = kırmızı build.

1. **Domain hiçbir şeye bağımlı değildir**: EF Core, MediatR, FluentValidation, Infrastructure
   isimleri Domain assembly'lerinde geçemez. Domain yalnızca BuildingBlocks.Domain'e bakabilir.
2. **Application, Infrastructure'a bağımlı olamaz**: Npgsql / EntityFrameworkCore Application
   assembly'lerinde geçemez. Veri erişimi repository arayüzleri (Domain'de tanımlı) üzerinden.
3. **Modül Infrastructure'ları birbirine bağımlı olamaz**: Identity.Infrastructure, Scheduling'i
   göremez (ve tersi). Paylaşım BuildingBlocks veya Contracts üzerinden.
4. **Api/Bootstrapper tipleri modüllerden referans alınamaz.**

## Pipeline davranış sırası (değiştirme)

Logging → Performance → Validation → Idempotency → Transaction.
`TransactionBehavior` yalnızca `ICommand<T>` isteklerinde katalog `IUnitOfWork.SaveChangesAsync`
çağırır. Tenant DB'ye yazan handler'lar `ISchedulingUnitOfWork`'ü KENDİLERİ kaydeder.

## Tenancy kuralları

- Tenant verisine dokunan her akış önce `ITenantDatabase.Set(...)` görmüş olmalı; aksi halde
  `TenantDbContext` bilinçli olarak fırlatır (sessiz yanlış-DB'ye yazmayı engeller).
- `TenantDatabaseProvisioner` idempotenttir: DB yoksa oluşturur, `MigrateAsync` koşar, varsayılan
  `CompanySettings` tohumlar. Tenant şemasına kolon/tablo eklerken YENİ migration üret
  (`dotnet ef migrations add X -p src/Modules/Scheduling/RowingClub.Scheduling.Infrastructure
  -s src/RowingClub.Api --context TenantDbContext -o Persistence/Migrations`).
- Katalog şeması değişirse ayrı migration: `--context RowingClubDbContext`,
  `-p src/BuildingBlocks/RowingClub.BuildingBlocks.Infrastructure -o Postgres/Migrations`.

## Kişisel veri

Üye/eğitmen PII'ı (ad, telefon, e-posta, not) tenant DB'de AES-256-GCM şifreli saklanır
(`TenantDbContext` value converter'ları). Aranabilir alan gerekiyorsa blind index deseni kullan
(`PhoneIndex` örneği). Yeni PII kolonu eklerken şifrelemeden düz metin bırakma.
