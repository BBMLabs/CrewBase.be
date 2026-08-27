# İndeksleme Rehberi

Mevcut indeksler ve gerekçeleri - yenilerini aynı mantıkla ekle:

| Tablo | İndeks | Neden |
|---|---|---|
| identity_companies | Name UNIQUE | firma adı tekilliği |
| identity_companies | Subdomain UNIQUE | subdomain → firma çözümü (her public istekte) |
| customers | PhoneIndex UNIQUE | şifreli telefonun blind-index araması + tekillik |
| training_sessions | (Date, StartTime) | gün/slot sorguları (availability, çakışma) |
| appointments | (SessionId, CustomerId) UNIQUE WHERE Status<>'Cancelled' | aynı üye aynı seansa iki kez giremez |
| appointments | (Date, StartTime) | gün görünümü + hatırlatma taraması |

Kurallar:

- Şifreli kolona indeks anlamsızdır; aranacak şifreli alan için normalize → HMAC → ayrı kolon.
- Filtered unique index, "aktif kayıtlar arasında tekil" kuralları için tercih edilir;
  uygulama kodundaki kontrol yarış koşullarında tek başına yetmez.
- Tenant DB'ler küçüktür; ölçüsüz indeks ekleme - yalnızca gerçek sorgu yollarına.
- Yeni indeks = yeni migration; her iki context'in de kendi zinciri var.
