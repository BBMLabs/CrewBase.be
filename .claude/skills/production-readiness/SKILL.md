---
name: production-readiness
description: Yayına çıkmadan önce veya deploy/konfigürasyon değişikliklerinde kullanılır. Env değişkenleri, docker-compose, migration sırası ve go-live kontrol listesi.
---

# Production Readiness

## Konfigürasyon

Tüm ayarlar env'den gelir (`DotEnvFileLoader` → `.env.developer` / `.env.production`,
`APP_ENV` seçer). Kritik gruplar:

- `POSTGRES_*` (host/port/db/user/pass) - katalog DB; tenant DB'ler aynı sunucuda otomatik.
- `REDIS_*` - idempotency store (Redis düşse bile app ayakta kalır, AbortOnConnectFail=false).
- `JWT_*` / RSA anahtarları, `ENCRYPTION_*` (alan şifreleme + blind index anahtarları) -
  **anahtar kaybı = şifreli veriler geri döndürülemez**; yedekle, rotasyonu KeyVersion ile yap.
- `SMTP_*` - hoş geldin + hatırlatma e-postaları. Yanlışsa kayıt yine çalışır (hata yutulup
  audit'e yazılır) ama e-posta çıkmaz.
- `AUTH_RATE_LIMIT` - IP başına dakikalık auth istek limiti.

## Açılış sırası (değiştirme)

1. Katalog migration (`EfMigrationHostedService`)
2. Tenant migration taraması (`TenantMigrationHostedService` - firma başına, hata izole)
3. `ReminderWorker` dakikalık tur.

## Go-live kontrol listesi

- [ ] `dotnet test` yeşil; `dotnet publish -c Release` deneniyor.
- [ ] .env.production güncel; yeni eklenen env anahtarları docker-compose'a işlendi
      (geçmişte JWT/FieldEncryption env'lerinin compose'a taşınmaması prod'u düşürdü).
- [ ] Migration'lar idempotent; dolu tabloya NOT NULL + unique için backfill var.
- [ ] Postgres kullanıcısının `CREATEDB` yetkisi var (tenant provisioning için).
- [ ] `/health` 200; Scalar/OpenAPI yalnızca Development'ta.
- [ ] Gerçek domain'e geçişte: `*.faturebase.com` wildcard DNS + TLS; `TenantResolver.BaseDomain`
      tek noktadan değişir; reverse proxy Host başlığını iletmeli.
- [ ] Loglarda PII/secret sızıntısı yok; Prometheus scrape hedefi ayarlı.
- [ ] DB yedekleri tenant DB'leri de kapsıyor (yalnızca katalog değil!).
