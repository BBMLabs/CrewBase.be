# Deployment

## 1. Genel Yaklaşım

RowingClub, Docker Compose ile ayağa kaldırılabilecek şekilde tasarlanmıştır. `deploy/docker/docker-compose.yml` şu servisleri içerir:

| Servis | Amaç |
|---|---|
| `api` | RowingClub.Api — ASP.NET Core Minimal API host'u |
| `mongodb` | MongoDB — platformun **tek** veri deposu; `--replSet rs0` ile çalışır, healthcheck ilk ayağa kalkışta tek node'luk replica set'i idempotent şekilde initiate eder (bkz. [ARCHITECTURE.md §9](./ARCHITECTURE.md#9-mongodb-veri-modeli)) |
| `redis` | Dağıtık cache, idempotency, rate limiting verisi, distributed lock |
| `prometheus` | Metric toplama |
| `grafana` | Dashboard ve görselleştirme |

Oracle veya başka bir ilişkisel veritabanı servisi yoktur — mimari kararın gerekçesi için bkz. [adr/0002-mongodb-tek-veritabani-stratejisi.md](./adr/0002-mongodb-tek-veritabani-stratejisi.md). Bu doküman `docker-compose.yml`'in kesin içeriğini tekrarlamaz, deployment stratejisinin genel kurallarını tanımlar; production'da `docker-compose.yml`'in kendisi (yalnızca lokal geliştirme için, sertleştirme içermez) kullanılmaz — bkz. dosyanın başındaki uyarı notu.

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

- Migration'lar **otomatik** çalışır: `MongoMigrationHostedService`, DI'a kayıtlı tüm `IMongoMigration` implementasyonlarını (`IEnumerable<IMongoMigration>`, tüm modüllerden toplanır) `Version`'a göre sıralayıp API host'u istek almaya başlamadan önce çalıştırır. Zaten uygulanmış versiyonlar `schema_migrations` koleksiyonundaki kayıttan tespit edilip atlanır — aynı migration seti her deploy'da güvenle tekrar çalıştırılabilir (idempotent).
- `dotnet ef database update` gibi manuel/ayrı bir migration adımı **yoktur** — EF Core kullanılmıyor. Bir migration'ın uygulanıp uygulanmadığını görmek için `schema_migrations` koleksiyonu sorgulanır.
- Her migration, bir koleksiyonu (yoksa) `$jsonSchema` validator'ıyla oluşturur ve ilgili indeksleri kurar; migration versiyon aralıkları modül başına ayrılmıştır (bkz. [DATA_MODEL.md](./DATA_MODEL.md#migration-versiyon-aralıkları)).
- Migration gerektiren bir domain değişikliği migration'sız merge edilemez; zaten yayınlanmış bir migration'ın içeriği **asla değiştirilmez** — bir düzeltme gerekiyorsa yeni, bir sonraki versiyon numarasıyla migration eklenir (EF Core migration'larındaki disiplinin aynısı).
- Geriye dönük uyumluluk: mümkün olduğunca **expand/contract** deseni izlenir (önce yeni alan eklenir ve iki sürüm kod tarafından da desteklenir, eski alan daha sonraki bir sürümde temizlenir) — böylece rolling deployment sırasında eski/yeni kod aynı doküman şekliyle çalışabilir. MongoDB'nin şemasız doğası bunu SQL'e göre kolaylaştırır: yeni alan eklemek var olan dokümanları etkilemez, `$jsonSchema`'daki `required` listesi yalnızca yeni yazılan dokümanlar için geçerlidir.

## 6. Rollback

- Container image'ları versiyonlanır (ör. semver veya commit SHA tag'i); bir dağıtımda sorun tespit edilirse bir önceki image tag'ine dönülür.
- **Mongo migration'ları SQL migration'ları gibi geri alınamaz.** Bir `IMongoMigration` için "down" script'i yoktur — bir index'i düşürmek, bir `$jsonSchema` validator'ını gevşetmek veya bir alanı geri eski haline getirmek, önceki migration'ı geri almak yerine **fix-forward**: yeni bir versiyon numarasıyla düzeltici bir migration eklenir. Bu, migration'ların tek yönlü, sıralı bir geçmiş oluşturduğu `schema_migrations` modeliyle tutarlıdır.
- Bu yüzden migration'lar mümkün olduğunca **geriye uyumlu ve additive** yazılır (expand/contract): bir önceki image tag'ine rollback edildiğinde, o eski kod hâlâ (migration'ın eklediği yeni alan/index'i görmezden gelerek) doğru çalışabilmelidir. Kod tarafında bir alanı zorunlu kılmadan önce, o alanı yazan migration'ın tüm production trafiğine yayılmış olduğundan emin olunur.
- Rollback öncesi, rollback edilecek sürümün outbox'ta bekleyen mesajlarla uyumluluğu (payload şeması) kontrol edilir.

## 7. Health Check

- `api` servisi ASP.NET Core Health Checks middleware'i ile en az şu bağımlılıkları kontrol eden bir health endpoint'i sağlar: MongoDB bağlantısı, Redis bağlantısı.
- Liveness ve readiness ayrımı yapılır: liveness yalnızca process'in ayakta olduğunu, readiness ise bağımlılıkların (DB, cache) erişilebilir olduğunu doğrular.
- Orkestrasyon katmanı (Docker Compose `healthcheck`, veya ileride Kubernetes probe'ları) bu endpoint'leri kullanarak trafik yönlendirme kararlarını verir.

## 8. Backup / Restore

- MongoDB, platformun tek veri deposu olduğu için tüm iş verisi (kullanıcı, kulüp, üyelik, ders/randevu, paket, refresh token, outbox/inbox, bildirim, raporlama snapshot'ları, audit) burada tutulur; düzenli, otomatik ve **şifreli** yedekler alınır (bkz. [SECURITY.md](./SECURITY.md#4-alan-bazlı-şifreleme-field-level-encryption)). Production'da bir replica set kullanıldığından, oplog tabanlı point-in-time recovery değerlendirilmelidir.
- Yedekten geri yükleme prosedürü periyodik olarak test edilir (tatbikat).
- Refresh token, outbox ve idempotency verisi gibi kısa ömürlü/operasyonel koleksiyonlar için backup önceliği, kullanıcı/kulüp/paket gibi kalıcı iş verisine göre daha düşüktür; ancak reuse-detection ve audit bütünlüğü için bu koleksiyonlar yine de yedeklenir.
- Restore sonrası, uygulamanın outbox/idempotency tutarlılığını (ör. yarım kalmış mesajların yeniden işlenmesi) doğru şekilde ele aldığından emin olunmalıdır — consumer'lar idempotent olduğu için bu senaryo güvenlidir. Restore edilen veritabanının bir replica set olarak (tek node dahi olsa `--replSet`) ayağa kalktığından emin olunmalıdır, aksi halde `MongoUnitOfWork`'ün transaction'ları çalışmaz.

## 9. Gözlemlenebilirlik Entegrasyonu

Deployment sonrası doğrulama, Grafana dashboard'ları ve health endpoint'leri üzerinden yapılır; detaylar için bkz. [OBSERVABILITY.md](./OBSERVABILITY.md).
