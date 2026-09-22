# Hata Formatı

İki katman vardır:

## 1. Endpoint seviyesinde erken dönüşler

```json
{ "success": false, "data": null, "message": "Tarih YYYY-AA-GG biçiminde olmalıdır.", "code": "invalid_date" }
```
`ApiResponse.Fail(code, mesaj)` + uygun HTTP kodu (400/404).

## 2. Handler'dan fırlayan hatalar (GlobalExceptionHandler → ProblemDetails)

```json
{
  "type": "https://errors.rowingclub.dev/slot_full",
  "title": "Conflict",
  "status": 409,
  "detail": "Bu saatte 2x teknelerinin tümü dolu. Lütfen başka bir saat seçin.",
  "instance": "/api/v1/public/x/appointments",
  "traceId": "...",
  "code": "slot_full"
}
```

- `DomainException(code, mesaj)` → 409 (veya handler'ın eşlemesine göre 4xx).
- `NotFoundException` → 404, `AuthenticationFailedException` → 401,
  FluentValidation → 400 `validation_error`, beklenmeyen → 500 `unexpected_error`.

## Kod sözlüğü (mevcutları yeniden kullan)

`company_not_found`, `company_name_taken`, `email_already_registered`, `slot_full`,
`session_full`, `already_booked`, `closed_day`, `too_soon`, `too_far`, `invalid_slot`,
`invalid_reminder`, `invalid_package`, `boat_class_unavailable`, `boat_taken`,
`instructor_busy`, `invalid_level`, `forbidden`, `invalid_role`, `cannot_demote_self`,
`guest_class_restricted`, `no_2x_partner_available`.

Yeni kod eklerken: snake_case, İngilizce, mesaj Türkçe ve kullanıcıya gösterilebilir olmalı.
