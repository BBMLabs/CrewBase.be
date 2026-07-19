# 0003 — Tenant İzolasyon Stratejisi

## Durum

Kabul edildi.

## Bağlam

RowingClub çok kiracılı (multi-tenant) bir platformdur: her kürek kulübü bir tenant'tır, bir kullanıcı birden fazla kulübe üye olabilir ve her kulüpte farklı role sahip olabilir. Ürün kuralı gereği, hiçbir kullanıcı başka bir kulübün verisine rol claim'i veya tahmin edilebilir ID kullanarak erişememelidir; her kulübün verisi diğerlerinden **kesin şekilde** izole edilmelidir.

Değerlendirilen tenant kimliği taşıma yöntemleri:

1. Yalnızca JWT içindeki bir `club_id`/rol claim'ine güvenmek.
2. Route parametresi (`{clubId}`) üzerinden tenant almak ve doğrudan güvenmek.
3. `X-Club-Id` header'ı ile aktif tenant'ı taşımak, ancak sunucu tarafında kullanıcının gerçek üyelikleriyle her istekte doğrulamak.

Salt JWT claim'ine güvenmek riskli kabul edildi çünkü: (a) bir kullanıcı birden fazla kulübe üye olabildiğinden "aktif kulüp" oturum/istek bazlı değişen bir bilgidir, token içine gömülü statik bir tenant claim'i bu modele uymaz; (b) token'lar üyelik iptal edildikten sonra da `exp` süresine kadar geçerli kalabilir — token içindeki bir rol/tenant claim'i üyelik güncel olmayan bir yetkiyi taşıyabilir.

## Karar

Aktif tenant, istek başına **`X-Club-Id` request header'ı** ile taşınır ve bu değer **istemciden geldiği için güvenilmez kabul edilir**. Sunucu tarafında aşağıdaki katmanlı doğrulama uygulanır:

1. `X-Club-Id` header'ındaki kulüp, kullanıcının **aktif üyelikleriyle sunucu tarafında** (Memberships modülü üzerinden) doğrulanır.
2. Route içindeki `{clubId}`, header'daki `X-Club-Id` ve doğrulanmış current tenant context'i **birbiriyle eşleşmek zorundadır**; uyuşmazlıkta istek reddedilir.
3. `ICurrentTenant` ve `ICurrentUser` abstraction'ları, doğrulanmış tenant/kullanıcı bilgisini request scope'unda taşır — handler'lar tenant bilgisini asla doğrudan header'dan veya DTO'dan okumaz.
4. **`MongoUnitOfWork.Track()`**, platformun tek veri deposu olan MongoDB'de tenant filtresinin uygulandığı noktadır: bir aggregate izlemeye alındığı anda (her load'da VE `Add`'de), `ITenantOwned` implemente ediyorsa `ICurrentTenant`'a karşı `ClubId` eşleşmesi kontrol edilir; uyuşmazlıkta `DomainException("tenant_mismatch", ...)` fırlatılır ve yazma engellenir. Bu, EF Core'daki global query filter + `SaveChanges` interceptor ikilisinin tek bir MongoDB-native mekanizmadaki karşılığıdır — ayrı bir "sorgu filtresi" ve ayrı bir "yazma doğrulaması" olarak değil, aggregate'in izlemeye alındığı tek noktada uygulanır.
5. Identity gibi platform seviyesinde (kulübe bağlı olmayan) modüllerin aggregate'leri `ITenantOwned` implemente etmez ve bu kontrole hiç tabi değildir; tenant'a bağlı modüller (Memberships, Scheduling, Packages, ...) implemente ettiğinde otomatik olarak devreye girer — repository'nin veya handler'ın ayrıca bir şey yapmasına gerek yoktur.
6. Platform Admin'in tenant sınırını aşan (bypass) işlemleri, özel bir policy (`PlatformAdminOnly` + ek onay) ve zorunlu audit kaydı gerektirir — sessiz bypass yoktur.
7. Tenant izolasyonu, `tests/RowingClub.SecurityTests` içinde otomatik regresyon testleriyle (yanlış tenant → 403/güvenli 404, tenant ID manipülasyonu → engellenmiş, IDOR denemesi → engellenmiş) sürekli doğrulanır.

Request işleme sırasında tenant çözümleme, authentication'dan hemen sonra ve endpoint authorization'dan önce gerçekleşir (bkz. [ARCHITECTURE.md §7](../ARCHITECTURE.md#7-tenant-çözümleme-akışı)).

## Sonuçlar

**Olumlu:**

- Tenant doğrulaması tek bir merkezi noktada (middleware + `ICurrentTenant` doldurma aşaması, ardından `MongoUnitOfWork.Track()`) yapıldığından, her endpoint'in kendi tenant kontrolünü elle yazma riski/unutma ihtimali azalır.
- Tek bir merkezi mekanizma (`Track()` içindeki `ITenantOwned` kontrolü) hem okuma hem yazma yolunda geçerli olduğundan, geliştiricinin "unutkanlıkla" tenant filtresiz bir sorgu yazmasını yapısal olarak zorlaştırır; EF Core'daki gibi ayrı bir global query filter'ı her yeni `DbSet` için hatırlayıp bağlamaya gerek yoktur.
- Çok kulüplü üyelik modeli (bir kullanıcının birden fazla kulüpte farklı rolleri) doğal olarak desteklenir; token'ın yeniden verilmesini gerektirmeden aktif kulüp istek bazında değişebilir.
- Platform Admin bypass'larının zorunlu audit'e tabi olması, ayrıcalıklı erişimin kötüye kullanımını caydırır ve iz bırakır.

**Olumsuz / riskler:**

- Her istekte ek bir "aktif üyelik doğrulama" sorgusu/lookup gerekir; bu, dikkatli cache'lenmezse (ör. kısa ömürlü Redis cache) ek gecikme getirebilir.
- Header tabanlı yaklaşım, istemci tarafının her istekte doğru `X-Club-Id`'yi göndermesini gerektirir; istemci hatası (yanlış/eksik header) kullanıcı deneyiminde `400`/`403` hatalarına yol açabilir — API dokümantasyonunda bu header açıkça zorunlu olarak işaretlenmelidir.
- `ITenantOwned` işaretlemesi geliştiricinin sorumluluğundadır: platform-geneli bir aggregate'e yanlışlıkla `ITenantOwned` eklemek (veya tenant'a bağlı bir aggregate'e eklemeyi unutmak) mimari testlerle sürekli izlenmelidir; `Track()` yalnızca `ITenantOwned` işaretli aggregate'leri kontrol eder, işaretlenmemiş bir aggregate sessizce kontrolsüz kalır.

## İlgili

- [ARCHITECTURE.md §7](../ARCHITECTURE.md#7-tenant-çözümleme-akışı)
- [SECURITY.md §6](../SECURITY.md#6-threat-model-temel) — tenant ID manipülasyonu tehdidi
- Ana geliştirme talimatı §7
