---
name: security-reviewer
description: Değişen kodu güvenlik gözüyle incelerken kullanılır. Bu repoya özgü kontrol listesi - tenant izolasyonu, yetki, injection, PII.
---

# Security Reviewer

Diff'i şu sırayla tara; her bulguda dosya:satır + somut saldırı senaryosu + düzeltme ver.

## 1. Tenant izolasyonu (bu repoda en kritik sınıf)

- Tenant verisine giden her yol `ITenantDatabase.Set` görmüş mü? Public uçta subdomain,
  panelde JWT CompanyId dışında bir kaynaktan tenant seçilebiliyor mu? (route/body'den
  companyId almak = ihlal)
- Bir firmanın ID'siyle başka firmanın kaydına ulaşılabiliyor mu (IDOR)? Handler hedef
  kaydın firmasını doğruluyor mu?

## 2. Yetkilendirme

- Yeni endpoint'te `RequireAuthorization` var mı, doğru rol mü?
- Rol yalnızca JWT claim'inden mi okunuyor? Firma-içi kritik işlemlerde DB'den yeniden
  doğrulama (`CompanyUserGuards` deseni) var mı?
- Privilege escalation: kullanıcı kendine/başkasına PlatformAdmin atayabilir mi?

## 3. Injection ve girdi

- SQL string birleştirme var mı? (izin verilen tek istisna: `CREATE DATABASE` +
  `EnsureSafeDatabaseName` doğrulaması) Yeni raw SQL parametreli mi?
- Template/HTML çıktısında kullanıcı verisi `HtmlEncoder`'dan geçiyor mu (XSS)?
- Dosya yolu/subdomain/DB adı gibi tanımlayıcılar whitelist regex'le doğrulanıyor mu?

## 4. PII ve sırlar

- Yeni kolon PII içeriyorsa şifreli mi? Loglara/audit'e PII veya token yazılıyor mu?
- Şifre/token düz metin dönülüyor veya e-postalanıyor mu? Hata mesajları kullanıcı varlığını
  sızdırıyor mu (user enumeration)?

## 5. Diğer

- Rate limit kapsamı dışında kalan hassas anonim uç var mı?
- Kriptografi: özel implementasyon yerine mevcut `AesGcmFieldEncryptor`/`HmacBlindIndexer`
  kullanılmış mı? Nonce tekrar kullanımı yok mu?
- Yarış koşulları: kapasite/tekillik kuralları DB kısıtıyla destekleniyor mu?
