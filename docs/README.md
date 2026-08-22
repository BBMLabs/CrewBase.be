# RowingClub (FatureBase) — Dokümantasyon İndeksi

Bu dizin, **RowingClub** (repo: `CrewBase.be`) kürek kulübü yönetim platformu backend'inin tüm
teknik dokümantasyonunu barındırır. Platform; kürek kulüplerinin üyelerini, eğitmenlerini,
derslerini, randevularını, ders paketlerini ve kulüp operasyonlarını yönetebildiği çok kiracılı
(`multi-tenant`, database-per-tenant) bir sistemdir. Sistemin tam ürün ve mimari gereksinimleri
için proje kökündeki `kurek-kulubu-claude-code-gelistirme-talimatlari.md` ana geliştirme talimatı
geçerlidir; bu dizindeki dokümanlar o talimatın mevcut kod tabanına yansıyan halini açıklar.

> **Bu proje yalnızca bir backend API'dir.** Ayrı bir frontend/web uygulaması bulunmaz. Her
> firmanın kayıt-randevu sitesi tek bir sunucu-render HTML şablonu olarak API içinden servis
> edilir (`GET /site/{subdomain}`). Bir istemci (mobil uygulama, üçüncü parti entegrasyon vb.)
> geliştirecekseniz `API-DOKUMANTASYONU.md`'den başlayın.

## Mevcut Durum (Özet)

- Çözüm (`RowingClub.sln`) modüler monolith: `RowingClub.Api`, `RowingClub.Bootstrapper`, 5 adet
  `BuildingBlocks` kütüphanesi, modüller (`Identity`, `Scheduling`, `Clubs`, `Memberships`,
  `Packages`, `Notifications`, `Reporting`), 5 test projesi.
- **Identity modülü** uçtan uca çalışır durumda: firma kaydı (otomatik aktif), giriş, refresh
  token rotation (reuse detection ile), logout/logout-all, OTP tabanlı e-posta doğrulama, şifre
  sıfırlama, platform admin yönetimi, firma-içi kullanıcı/rol yönetimi. 2FA altyapısı hazır ama
  endpoint'leri bilinçli olarak kapalıdır.
- **Scheduling modülü** uçtan uca çalışır durumda ve database-per-tenant mimarisinin merkezinde:
  üyeler, derece bazlı seans/tekne gruplaması, randevu+iptal+paket düşümü, çalışma saatleri/kapalı
  günler/hatırlatma ayarları, beyan (consent) yönetimi, Multisport/Meditopia üyelik kartları,
  arkadaşlık + SignalR ile gerçek zamanlı mesajlaşma, kulüp akışı (feed: paylaşım/etkinlik/beğeni/
  yorum/katılım/takip).
- **Clubs, Memberships, Packages, Notifications, Reporting** modülleri yalnızca proje iskeleti
  düzeyinde **planlanmış / scaffold edilmiş** durumda; iş mantığı henüz yazılmadı. (Not: ders
  paketi ve üye kavramları artık Identity/Scheduling içinde fiilen implemente edilmiştir — bu
  isimlerdeki ayrı modüller henüz açılmamış genişleme alanlarıdır.)

## Doküman Haritası

| Dosya | İçerik |
|---|---|
| [API-DOKUMANTASYONU.md](./API-DOKUMANTASYONU.md) | API'yi tüketecek istemciler için akış odaklı pratik rehber (auth, randevu, üye, chat, feed akışları) |
| [API_ENDPOINTS.md](./API_ENDPOINTS.md) | Tüm endpoint'lerin eksiksiz referansı: route, request/response şekli, iş kuralları, hata kodları |
| [AUTHORIZATION_MATRIX.md](./AUTHORIZATION_MATRIX.md) | Tüm endpoint'lerin rol/policy/tenant/sahiplik matrisi |
| [DEVELOPMENT.md](./DEVELOPMENT.md) | Lokal geliştirme kurulumu, gerekli SDK'lar, environment değişkenleri, Docker Compose, migration, test komutları, seed data, kod standartları, yeni özellik ekleme adımları |
| [ARCHITECTURE.md](./ARCHITECTURE.md) | Sistem bağlamı, bounded context'ler, katman sorumlulukları, DDD/CQRS yaklaşımı, tenant çözümleme akışı, outbox/inbox akışı, PostgreSQL/EF Core veri modeli |
| [SECURITY.md](./SECURITY.md) | Authentication, authorization modeli, secret yönetimi, alan bazlı şifreleme, key rotation, threat model, OWASP kontrolleri, audit, rate limiting, PII |
| [DATA_MODEL.md](./DATA_MODEL.md) | Modül bazlı aggregate/entity listesi, PostgreSQL tabloları, indeksler, EF Core migration'ları |
| [ODATA.md](./ODATA.md) | OData güvenlik kuralları: allow-list query option'lar, `$expand` kısıtı, maksimum `$top`, tenant filtresi zorunluluğu |
| [OBSERVABILITY.md](./OBSERVABILITY.md) | Log formatı ve zorunlu alanlar, metric listesi, trace span'leri, Grafana dashboard'ları, correlation id yayılımı |
| [DEPLOYMENT.md](./DEPLOYMENT.md) | Environment yapılandırması, secret injection, Docker/production deployment, migration/rollback, backup/restore, health check |
| [CHANGELOG.md](./CHANGELOG.md) | Sürüm geçmişi (Keep a Changelog formatı) |
| [adr/](./adr/) | Architecture Decision Record'lar |
| [diagrams/](./diagrams/) | Mimari diyagramlar |

## Modül Bağlantıları

| Modül | Durum | Katmanlar |
|---|---|---|
| Identity | Uçtan uca çalışıyor | Domain, Application, Infrastructure, Contracts |
| Scheduling | Uçtan uca çalışıyor (database-per-tenant) | Domain, Application, Infrastructure, Contracts |
| Clubs | Planlandı (scaffold) | Domain, Application, Infrastructure, Contracts |
| Memberships | Planlandı (scaffold) | Domain, Application, Infrastructure, Contracts |
| Packages | Planlandı (scaffold) — not: ders paketi kavramı Scheduling içinde fiilen var | Domain, Application, Infrastructure, Contracts |
| Notifications | Planlandı (scaffold) — not: e-posta/OTP/hatırlatma gönderimi Identity+Scheduling+Api içinde fiilen var | Domain, Application, Infrastructure, Contracts |
| Reporting | Planlandı (scaffold) | Application, Infrastructure |

Her modülün sorumlulukları ve ana nesneleri için bkz. [ARCHITECTURE.md](./ARCHITECTURE.md) ve
[DATA_MODEL.md](./DATA_MODEL.md).
