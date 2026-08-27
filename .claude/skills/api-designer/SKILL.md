---
name: api-designer
description: Bu repoda yeni REST endpoint tasarlarken kullanılır. URL/verb kuralları, ApiResponse zarfı, hata formatı ve yetkilendirme desenleri.
---

# API Designer

Endpoint eklerken `references/rest-conventions.md` ve `references/error-format.md`'yi izle.

## Yüzeyler

- `/api/v1/auth/*` — kimlik akışları (anonim + rate limit `RateLimitingSetup.AuthPolicy`).
- `/api/v1/public/{subdomain}/*` — firma sitesinin anonim uçları; ilk iş
  `TenantResolver.ResolveBySubdomainAsync`, firma yoksa 404 `company_not_found`.
- `/api/v1/company/*` — firma paneli; `Roles = "CompanyAdmin"`, firma JWT'deki CompanyId'den.
- `/api/v1/platform/*` — platform admin (`Roles = "PlatformAdmin"`).

## Zorunlu desenler

- Minimal API + `MapGroup`, her uca `.WithName("PascalCase")`.
- Request gövdesi endpoint dosyasında `sealed record XxxRequest(...)`.
- Başarı: `ApiResponse<T>.Ok(data, mesaj?)`; oluşturma: `Results.Created(uri, ...)`.
- Tarih `yyyy-MM-dd`, saat `HH:mm` string olarak alınır ve endpoint'te parse edilir;
  parse hatası 400 + `invalid_date`/`invalid_time`.
- İş kuralı hataları handler'dan `DomainException` olarak gelir; GlobalExceptionHandler bunları
  ProblemDetails'e çevirir - endpoint'te try/catch yazma.
- OpenAPI development'ta `/openapi` + Scalar UI; yeni auth ucu eklersen Functional testteki
  endpoint envanterini güncelle.
