# Veritabanı Kuralları

- PostgreSQL 15+ hedeflenir; bağlantı `PostgresOptions` (env: POSTGRES_*) üzerinden kurulur,
  tenant bağlantıları aynı sunucuda yalnızca Database adı değiştirilerek üretilir
  (`TenantConnectionStringFactory`).
- Tenant DB adı yalnızca `[a-z0-9_]`, 63 karakter sınırı; `EnsureSafeDatabaseName` doğrular
  (SQL injection önlemi - CREATE DATABASE parametre alamaz).
- Tablo adları snake_case (`training_sessions`), katalog tablolarında modül öneki
  (`identity_companies`).
- Soft-delete yerine `IsActive`/`Status` alanları; fiziksel silme yalnızca cascade ilişkilerde
  (appointment → customer/session).
- FK davranışları bilinçli seçilir: üye silinirse randevuları gitsin (`Cascade`), tekne/eğitmen
  silinirse seans kalsın (`SetNull`).
- Concurrency: katalog aggregate'lerinde `Version` (optimistic); tenant tarafında kapasite
  yarışları filtered unique index'lerle güvenceye alınır.
- Tek satırlık ayar tabloları (company_settings) provision sırasında tohumlanır; okurken
  `?? CompanySettings.Default()` fallback'i korunur.
