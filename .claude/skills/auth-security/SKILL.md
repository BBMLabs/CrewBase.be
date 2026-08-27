---
name: auth-security
description: Kimlik doğrulama/yetkilendirme değişikliklerinde kullanılır. JWT, roller, firma sınırı, şifre/token saklama ve alan şifreleme kuralları.
---

# Auth & Security

## Kimlik mimarisi

- JWT: RSA imzalı (`RsaJwtTokenService`), claim'ler: UserId, Email, Role, CompanyId.
  Refresh token: opak, SHA-256 hash'lenmiş, aile (family) takibi + yeniden kullanım tespiti.
- Şifreler Argon2id (`Argon2IdPasswordHasher`); düz metin şifre asla loglanmaz, E-POSTAYLA
  GÖNDERİLMEZ (hoş geldin maili şifre içermez - bilinçli karar).
- Roller: `PlatformAdmin`, `CompanyAdmin`, `Employee`. 2FA endpoint'leri şu an bilinçli pasif.

## Yetkilendirme kuralları (hepsi backend'de)

1. Rol kontrolü endpoint'te `RequireAuthorization(Roles=...)` İLE BAŞLAR ama BİTMEZ:
   firma-içi işlemlerde handler, çağıranı DB'den yükleyip rolünü ve CompanyId eşleşmesini
   YENİDEN doğrular (`CompanyUserGuards.EnsureCallerIsCompanyAdminAsync` deseni).
2. Firma sınırı: hedef kayıt çağıranın firmasına ait değilse `forbidden` - istemciden gelen
   companyId'ye asla güvenme; her zaman JWT/DB'den al.
3. Firma admini yalnızca `CompanyAdmin`/`Employee` verebilir; `PlatformAdmin` atanamaz.
   Kendi admin yetkisini düşüremez (`cannot_demote_self`).
4. Tenant izolasyonu: panel istekleri yalnızca kendi tenant DB'sine gider
   (`TenantResolver.ResolveByCompanyIdAsync` → `ITenantDatabase`).

## Veri koruması

- PII tenant DB'de AES-256-GCM şifreli; arama HMAC blind index. Yeni PII alanı eklerken
  `TenantDbContext`'teki converter desenini kullan.
- Anahtarlar env'den (`EncryptionOptions`, JWT anahtarları); koda/log'a anahtar yazma.
- Frontend'e öneri: token'ları localStorage yerine memory + httpOnly cookie'de tut; cache/
  localStorage/sessionStorage'a PII yazılacaksa şifrele - API yanıtlarında bu yüzden PII'ı
  gereken minimumda tut.
- Rate limit: auth uçları IP başına dakikalık pencere (`AUTH_RATE_LIMIT` env).
- Audit: hassas işlemler `IAuditLogger` ile loglanır (LOGIN_*, COMPANY_USER_*).
