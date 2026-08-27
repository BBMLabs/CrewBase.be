---
name: frontend-security
description: Frontend'de kimlik, depolama ve veri sızıntısı kararlarında kullanılır. Token/PII saklama ilkeleri ve panel yetki sınırları.
---

# Frontend Security

## Depolama ilkesi (tartışmaya kapalı)

1. **PII tarayıcıda saklanmaz.** Ad, telefon, e-posta, randevu listesi vb. yalnızca React
   state'inde yaşar; localStorage/sessionStorage/IndexedDB/cache'e YAZILMAZ. Sayfa yenilenince
   API'den tekrar çekilir - "hızlansın diye cache'leyeyim" deme.
2. **Oturum:** yalnızca JWT + rol etiketi `sessionStorage`'da (`fb.session`) tutulur; sekme
   kapanınca ölür. localStorage'a taşıma (XSS'te kalıcı çalınır). Eğer ileride bir şeyin istemcide
   saklanması şart olursa Web Crypto AES-GCM ile şifrelenmeli ve anahtar depoda TUTULMAMALIDIR;
   anahtarı yanına koymak şifreleme değildir.
3. JWT içeriği yalnızca rol yönlendirmesi için decode edilir; içindeki veriye güven duyulmaz -
   asıl doğrulama backend'dedir.

## Yetki

- `RequireRole` UI korumasıdır, güvenlik sınırı DEĞİLDİR; her hassas isteğin gerçek denetimi
  backend'de yapılır. UI'da gizlemek yetmiyorsa backend'e kural eklet.
- Üye ekranlarında başka üyelerin verisi yalnızca maskeli gelir ("Veli D."); frontend'de tam veri
  isteyen bir ekran gerekiyorsa önce backend sözleşmesini sorgula - maskeyi istemcide kaldırmaya
  çalışma (veri zaten gelmiyor olmalı).

## Girdi/çıktı

- Kullanıcı verisini asla `dangerouslySetInnerHTML` ile basma; React'in varsayılan kaçışına güven.
- URL kurarken kullanıcı girdisini `encodeURIComponent`'ten geçir (BookingWidget'taki phone örneği).
- Hata mesajlarında sunucudan gelen `message` gösterilir; stack/iç detay gösterme.
- Konsola token, telefon, e-posta loglama - `console.log` kalıntılarını temizle.

## Ağ

- API adresi `VITE_API_URL`; üretimde HTTPS zorunlu. Kimlik yalnızca `Authorization: Bearer`
  başlığında taşınır; cookie/`credentials: include` kullanılmaz (CSRF yüzeyi açma).
