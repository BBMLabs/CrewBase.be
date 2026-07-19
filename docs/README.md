# RowingClub — Dokümantasyon İndeksi

Bu dizin, **RowingClub** (repo: `CrewBase.be`) kürek kulübü yönetim platformu backend'inin tüm teknik dokümantasyonunu barındırır. Platform; kürek kulüplerinin üyelerini, eğitmenlerini, derslerini, randevularını, ders paketlerini ve kulüp operasyonlarını yönetebildiği çok kiracılı (`multi-tenant`) bir sistemdir. Sistemin tam ürün ve mimari gereksinimleri için proje kökündeki `kurek-kulubu-claude-code-gelistirme-talimatlari.md` ana geliştirme talimatı geçerlidir; bu dizindeki dokümanlar o talimatın mevcut kod tabanına yansıyan halini açıklar.

## Mevcut Durum (Özet)

- Çözüm iskeleti (`RowingClub.sln`) modüler monolith olarak tamamen kuruldu ve derleniyor: `RowingClub.Api`, `RowingClub.Bootstrapper`, 5 adet `BuildingBlocks` kütüphanesi, 6 modül (`Identity`, `Clubs`, `Memberships`, `Scheduling`, `Packages`, `Notifications`) ve `Reporting` modülü (Application/Infrastructure), 5 test projesi.
- **Identity modülü** uçtan uca implementasyon aşamasında: kayıt, giriş, refresh token rotation (reuse detection ile), logout, logout-all. Bu modül **implemente ediliyor**.
- **Clubs, Memberships, Scheduling, Packages, Notifications, Reporting** modülleri yalnızca proje iskeleti düzeyinde **planlanmış / scaffold edilmiş** durumda; iş mantığı henüz yazılmadı.

## Doküman Haritası

| Dosya | İçerik |
|---|---|
| [DEVELOPMENT.md](./DEVELOPMENT.md) | Lokal geliştirme kurulumu, gerekli SDK'lar, environment değişkenleri, Docker Compose, migration, test komutları, seed data, kod standartları, yeni özellik ekleme adımları |
| [ARCHITECTURE.md](./ARCHITECTURE.md) | Sistem bağlamı, bounded context'ler, katman sorumlulukları, DDD/CQRS yaklaşımı, tenant çözümleme akışı, outbox/inbox akışı, MongoDB veri modeli |
| [SECURITY.md](./SECURITY.md) | Authentication, authorization modeli, secret yönetimi, alan bazlı şifreleme, key rotation, threat model, OWASP kontrolleri, audit, rate limiting, PII |
| [DATA_MODEL.md](./DATA_MODEL.md) | Modül bazlı aggregate/entity listesi, MongoDB koleksiyonları, indeksler, `$jsonSchema` doğrulama |
| [ODATA.md](./ODATA.md) | OData güvenlik kuralları: allow-list query option'lar, `$expand` kısıtı, maksimum `$top`, tenant filtresi zorunluluğu |
| [OBSERVABILITY.md](./OBSERVABILITY.md) | Log formatı ve zorunlu alanlar, metric listesi, trace span'leri, Grafana dashboard'ları, correlation id yayılımı |
| [DEPLOYMENT.md](./DEPLOYMENT.md) | Environment yapılandırması, secret injection, Docker/production deployment, migration/rollback, backup/restore, health check |
| [CHANGELOG.md](./CHANGELOG.md) | Sürüm geçmişi (Keep a Changelog formatı) |
| [adr/](./adr/) | Architecture Decision Record'lar |
| [diagrams/](./diagrams/) | Mimari diyagramlar |

## Henüz Yazılmamış Dokümanlar

Aşağıdaki iki doküman, ana geliştirme talimatının 15. ve 16. bölümlerinde zorunlu tutulmuştur ve **Identity modülünü implemente eden mühendis tarafından**, endpoint sözleşmeleri kesinleştikçe eklenecektir. Bu dizin şu anda bu dosyaları içermez:

- `docs/API_ENDPOINTS.md` — Tüm endpoint'lerin request/response, validation, hata kodu detayları.
- `docs/AUTHORIZATION_MATRIX.md` — Tüm endpoint'lerin rol/policy/permission/tenant/sahiplik matrisi.

Bu iki dosya eklenene kadar, endpoint bazlı yetkilendirme detayları için ana geliştirme talimatının 12, 14 ve 15. bölümlerine bakılmalıdır.

## Modül Bağlantıları

| Modül | Durum | Katmanlar |
|---|---|---|
| Identity | İmplemente ediliyor | Domain, Application, Infrastructure, Contracts |
| Clubs | Planlandı (scaffold) | Domain, Application, Infrastructure, Contracts |
| Memberships | Planlandı (scaffold) | Domain, Application, Infrastructure, Contracts |
| Scheduling | Planlandı (scaffold) | Domain, Application, Infrastructure, Contracts |
| Packages | Planlandı (scaffold) | Domain, Application, Infrastructure, Contracts |
| Notifications | Planlandı (scaffold) | Domain, Application, Infrastructure, Contracts |
| Reporting | Planlandı (scaffold) | Application, Infrastructure |

Her modülün sorumlulukları ve ana nesneleri için bkz. [ARCHITECTURE.md](./ARCHITECTURE.md) ve [DATA_MODEL.md](./DATA_MODEL.md).
