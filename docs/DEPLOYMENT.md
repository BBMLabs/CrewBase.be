# Deployment

## 1. Genel Yaklaşım

RowingClub, Docker Compose ile ayağa kaldırılabilecek şekilde tasarlanmıştır. `deploy/docker/docker-compose.yml` şu servisleri içerir:

| Servis | Amaç |
|---|---|
| `api` | RowingClub.Api — ASP.NET Core Minimal API host'u |
| `postgresql` | PostgreSQL — platformun **tek** veri deposu; tek node üzerinde gerçek ACID transaction desteği sağlar, MongoDB'deki gibi bir replica set kurulumu gerekmez (bkz. [ARCHITECTURE.md §9](./ARCHITECTURE.md#9-postgresql-ve-ef-core-veri-modeli)) |
| `redis` | Dağıtık cache, idempotency, rate limiting verisi, distributed lock |
| `prometheus` | Metric toplama |
| `grafana` | Dashboard ve görselleştirme |

Oracle veya MongoDB gibi başka bir veritabanı servisi yoktur — mimari kararın gerekçesi için bkz. [adr/0004-postgresql-tek-veritabani-stratejisi.md](./adr/0004-postgresql-tek-veritabani-stratejisi.md). Bu doküman `docker-compose.yml`'in kesin içeriğini tekrarlamaz, deployment stratejisinin genel kurallarını tanımlar; production'da `docker-compose.yml`'in kendisi (yalnızca lokal geliştirme için, sertleştirme içermez) kullanılmaz — bkz. dosyanın başındaki uyarı notu.

## 2. Environment Yapılandırması

- Ortam bazlı yapılandırma `.env.developer` (lokal) ve `.env.product` (production) dosyaları ile yönetilir; her ikisi de Git'e eklenmez.
- Repository'de yalnızca `.env.example` bulunur ve tüm zorunlu değişkenlerin adlarını (değersiz) listeler (bkz. [DEVELOPMENT.md §2](./DEVELOPMENT.md#2-environment-değişkenleri)).
- `APP_ENV` değişkeni ortamı belirler (`Development`, `Staging`, `Production` vb.); uygulama davranışı (Swagger erişimi, seed data, log seviyesi) bu değere göre farklılaşır.
- Uygulama, zorunlu bir environment değişkeni/secret eksikse **başlamayı reddeder** (fail-fast); production'da sessiz varsayılan değerlerle çalışmaz.

## 3. Secret Injection Stratejisi

Production ortamında secret değerleri **repository'den değil**, aşağıdaki mekanizmalardan biri üzerinden enjekte edilir:

- CI/CD secret store (ör. GitHub Actions Secrets, Azure DevOps Variable Groups)
- Docker/Kubernetes secrets
- HashiCorp Vault veya benzeri bir secret manager
- Cloud provider secret manager (ör. Azure Key Vault, AWS Secrets Manager)

Kurallar:

- Secret değerleri container image'ına asla build-time'da gömülmez; runtime'da environment variable veya mount edilen secret dosyası olarak sağlanır.
- JWT imzalama anahtarları ve alan şifreleme anahtarları (`FIELD_ENCRYPTION_KEY_*`) rotation'ı destekleyecek şekilde versiyonlanmış olarak saklanır (bkz. [SECURITY.md §5](./SECURITY.md#5-key-rotation)).
- Secret'lar hiçbir log çıktısında görünmez.

## 4. Docker / Production Deployment

- `api` servisi çok aşamalı (multi-stage) bir Dockerfile ile derlenir: build aşamasında `dotnet publish`, çalışma aşamasında yalnızca published çıktı + ASP.NET Core runtime image kullanılır (SDK image production'da taşınmaz).
- Container güvenlik taraması (ör. Trivy) CI pipeline'ının bir parçasıdır (bkz. ana talimat §25 CI/CD kalite kapıları).
- Sağlıklı bir dağıtım, `api` servisinin health check'i geçmeden trafiğe alınmamasını garanti eder (bkz. §7).
- Yatay ölçekleme (birden fazla `api` instance'ı) için: rate limiting ve idempotency verisi Redis'te merkezi tutulur, oturum durumu (varsa) sticky session'a bağımlı olmayacak şekilde tasarlanır.

## 5. Migration Stratejisi

- Migration'lar **otomatik** çalışır: `EfMigrationHostedService`, API host'u istek almaya başlamadan önce `context.Database.MigrateAsync()` çağırarak henüz uygulanmamış EF Core migration'larını sırayla uygular. Zaten uygulanmış migration'lar EF'in kendi `__EFMigrationsHistory` tablosundaki kayıttan tespit edilip atlanır — aynı migration seti her deploy'da güvenle tekrar çalıştırılabilir (idempotent).
- Migration'lar **versiyonlu SQL migration'larıdır**: `dotnet ef migrations add <Ad>` ile üretilir, `src/BuildingBlocks/RowingClub.BuildingBlocks.Infrastructure/Postgres/Migrations/` altında dosya olarak saklanır ve koda commit edilir (bkz. [DATA_MODEL.md — EF Core Migrations](./DATA_MODEL.md#ef-core-migrations)). Manuel olarak uygulamak isteyen bir operatör `dotnet ef database update` çalıştırabilir, ancak normal akışta buna gerek yoktur — `EfMigrationHostedService` bunu otomatik yapar.
- Her migration, ilgili modülün `IEntityTypeConfiguration<T>` tanımlarından EF Core tarafından otomatik üretilen bir diff'tir (tablo/sütun/indeks/foreign key oluşturma-değiştirme); modül başına ayrılmış manuel bir versiyon aralığı şeması **yoktur** — sıralama dosya adındaki timestamp'e göre, `__EFMigrationsHistory` tablosunca izlenir.
- Migration gerektiren bir domain değişikliği migration'sız merge edilemez; zaten yayınlanmış (production'a gitmiş) bir migration'ın içeriği **asla değiştirilmez** — bir düzeltme gerekiyorsa yeni bir migration eklenir.
- Geriye dönük uyumluluk: mümkün olduğunca **expand/contract** deseni izlenir (önce yeni sütun eklenir ve iki sürüm kod tarafından da desteklenir, eski sütun daha sonraki bir sürümde temizlenir) — böylece rolling deployment sırasında eski/yeni kod aynı şemayla çalışabilir. SQL migration'ları MongoDB'nin şemasız dokümanlarının aksine sütun ekleme/kaldırmayı da versiyonlu bir adım olarak ele alır; `NOT NULL` bir sütun eklemek önce nullable + backfill + sonra `NOT NULL`'a geçiş gibi çok adımlı bir migration dizisi gerektirebilir.

## 6. Rollback

- Container image'ları versiyonlanır (ör. semver veya commit SHA tag'i); bir dağıtımda sorun tespit edilirse bir önceki image tag'ine dönülür.
- EF Core migration'ları teknik olarak `Down()` metoduyla geri alınabilir (`dotnet ef database update <öncekiMigrationAdı>`), ancak production'da bir migration'ı geri almak (özellikle veri kaybına yol açabilecek `DROP COLUMN`/`DROP TABLE` içeren migration'larda) riskli kabul edilir; tercih edilen yaklaşım hâlâ **fix-forward**: yeni bir migration ile düzeltme eklenir, önceki migration'ın kendisi geri alınmaz.
- Bu yüzden migration'lar mümkün olduğunca **geriye uyumlu ve additive** yazılır (expand/contract): bir önceki image tag'ine rollback edildiğinde, o eski kod hâlâ (migration'ın eklediği yeni sütun/indeksi görmezden gelerek) doğru çalışabilmelidir. Kod tarafında bir sütunu zorunlu kılmadan önce, o sütunu yazan migration'ın tüm production trafiğine yayılmış olduğundan emin olunur.
- Rollback öncesi, rollback edilecek sürümün outbox'ta bekleyen mesajlarla uyumluluğu (payload şeması) kontrol edilir.

## 7. Health Check

- `api` servisi ASP.NET Core Health Checks middleware'i ile en az şu bağımlılıkları kontrol eden bir health endpoint'i sağlar: PostgreSQL bağlantısı (`PostgresHealthCheck`, `SELECT 1` çalıştırır), Redis bağlantısı.
- Liveness ve readiness ayrımı yapılır: liveness yalnızca process'in ayakta olduğunu, readiness ise bağımlılıkların (DB, cache) erişilebilir olduğunu doğrular.
- Orkestrasyon katmanı (Docker Compose `healthcheck`, veya ileride Kubernetes probe'ları) bu endpoint'leri kullanarak trafik yönlendirme kararlarını verir.

## 8. Backup / Restore

- PostgreSQL, platformun tek veri deposu olduğu için tüm iş verisi (kullanıcı, kulüp, üyelik, ders/randevu, paket, refresh token, outbox/inbox, bildirim, raporlama snapshot'ları, audit) burada tutulur; düzenli, otomatik ve **şifreli** yedekler alınır (bkz. [SECURITY.md](./SECURITY.md#4-alan-bazlı-şifreleme-field-level-encryption)). Mantıksal yedekleme için `pg_dump`/`pg_restore`, fiziksel/point-in-time recovery için `pg_basebackup` + WAL arşivleme genel PostgreSQL pratiğidir; bu repoda henüz somut bir yedekleme aracı/otomasyonu konfigüre edilmemiştir, seçilecek yaklaşım production altyapısı netleştikçe belirlenecektir.
- Yedekten geri yükleme prosedürü periyodik olarak test edilir (tatbikat).
- Refresh token, outbox ve idempotency verisi gibi kısa ömürlü/operasyonel tablolar için backup önceliği, kullanıcı/kulüp/paket gibi kalıcı iş verisine göre daha düşüktür; ancak reuse-detection ve audit bütünlüğü için bu tablolar yine de yedeklenir.
- Restore sonrası, uygulamanın outbox/idempotency tutarlılığını (ör. yarım kalmış mesajların yeniden işlenmesi) doğru şekilde ele aldığından emin olunmalıdır — consumer'lar idempotent olduğu için bu senaryo güvenlidir. Restore edilen veritabanının şemasının, uygulamanın beklediği EF Core migration seviyesiyle (`__EFMigrationsHistory`) uyumlu olduğundan emin olunmalıdır; gerekiyorsa restore sonrası `context.Database.MigrateAsync()` (uygulama başlangıcında zaten otomatik çalışır) eksik migration'ları tamamlar.

## 9. Gözlemlenebilirlik Entegrasyonu

Deployment sonrası doğrulama, Grafana dashboard'ları ve health endpoint'leri üzerinden yapılır; detaylar için bkz. [OBSERVABILITY.md](./OBSERVABILITY.md).
