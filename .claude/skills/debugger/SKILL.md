---
name: debugger
description: Hata ayıklarken kullanılır. Bu repoda loglara/izlere nereden bakılır, tipik arıza kalıpları ve uçtan uca doğrulama yöntemi.
---

# Debugger

## Bilgi kaynakları

- Serilog console çıktısı: her istek `CorrelationId` taşır; hata yanıtındaki `traceId` ile
  log satırını eşleştir.
- ProblemDetails `code` alanı hatanın sınıfını söyler (`error-format.md` sözlüğü).
- Audit logları: `AUDIT [EVENT]` satırları (login, kullanıcı yönetimi).
- `/health` → Postgres bağlantısı; Prometheus metrikleri ve OpenTelemetry izleri açık.

## Tipik arıza kalıpları

1. **"Tenant veritabanı bu istek için çözülmedi"** → akış `ITenantDatabase.Set` görmeden
   tenant repo'suna ulaşmış. Endpoint'te resolver çağrısı eksik veya scope karışmış
   (arka plan işinde firma başına YENİ scope açılmalı - `ReminderWorker` deseni).
2. **Migration açılışta patlıyor** → hangi zincir? Katalog: `EfMigrationHostedService`;
   tenant: `TenantMigrationHostedService` (firma bazlı loglar). Tenant DB'si migration
   geçmişi olmadan (eski EnsureCreated) kalmışsa drop + yeniden provision gerekir.
3. **Şifreli kolonda arama boş dönüyor** → düz kolona LINQ Where yazılmıştır; blind index
   üzerinden arat (`CustomerRepository.GetByPhoneAsync`).
4. **401/403 beklenmedik** → JWT claim'leri mi eksik (role/companyId), yoksa handler'daki
   DB doğrulaması mı reddediyor? İkisi ayrı katman.
5. **Türkçe karakterli JSON bozuk** → curl'de UTF-8 sorunu; payload'ı dosyaya yazıp
   `--data-binary @file` kullan.

## Yöntem

Belirtiden hipoteze: hatayı yeniden üret (curl), logdan traceId'yi bul, ilgili handler'ı oku,
hipotezini tek değişiklikle test et. Uygulamayı arka planda `dotnet run` ile başlat,
`/health` bekle, akışı curl ile sür. Düzeltmeden sonra `dotnet test` + aynı curl senaryosu.
