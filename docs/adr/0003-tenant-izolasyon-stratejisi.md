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
4. **`RowingClubDbContext.SaveChangesAsync`**, platformun tek veri deposu olan PostgreSQL'de tenant filtresinin uygulandığı noktadır: `SaveChanges` çağrıldığında, `ChangeTracker.Entries<ITenantOwned>()` üzerinden değişen (Added/Modified) her entity, `ICurrentTenant`'a karşı `ClubId` eşleşmesi için kontrol edilir; uyuşmazlıkta `DomainException("tenant_mismatch", ...)` fırlatılır ve yazma engellenir. Bu kontrol yalnızca **yazma anında** uygulanır — şu an `ClubId` üzerinde global bir EF Core query filter'ı (okuma yolunu da otomatik filtreleyecek) **yoktur**; okuma yolunda tenant filtresi repository sorgularının kendisi tarafından uygulanmalıdır. Global query filter eklemek, bu ADR'nin kapsamı dışında, ileride değerlendirilebilecek bir sertleştirme adımıdır.
5. Identity gibi platform seviyesinde (kulübe bağlı olmayan) modüllerin aggregate'leri `ITenantOwned` implemente etmez ve bu kontrole hiç tabi değildir; tenant'a bağlı modüller (Memberships, Scheduling, Packages, ...) implemente ettiğinde otomatik olarak devreye girer — repository'nin veya handler'ın ayrıca bir şey yapmasına gerek yoktur.
6. Platform Admin'in tenant sınırını aşan (bypass) işlemleri, özel bir policy (`PlatformAdminOnly` + ek onay) ve zorunlu audit kaydı gerektirir — sessiz bypass yoktur.
7. Tenant izolasyonu, `tests/RowingClub.SecurityTests` içinde otomatik regresyon testleriyle (yanlış tenant → 403/güvenli 404, tenant ID manipülasyonu → engellenmiş, IDOR denemesi → engellenmiş) sürekli doğrulanır.

Request işleme sırasında tenant çözümleme, authentication'dan hemen sonra ve endpoint authorization'dan önce gerçekleşir (bkz. [ARCHITECTURE.md §7](../ARCHITECTURE.md#7-tenant-çözümleme-akışı)).

## Sonuçlar

**Olumlu:**

- Tenant doğrulaması tek bir merkezi noktada (middleware + `ICurrentTenant` doldurma aşaması, ardından `RowingClubDbContext.SaveChangesAsync`) yapıldığından, her endpoint'in kendi tenant yazma-kontrolünü elle yazma riski/unutma ihtimali azalır.
- Tek bir merkezi mekanizma (`SaveChangesAsync` içindeki `ITenantOwned` kontrolü), her yeni `DbSet`/entity tipi için ayrı ayrı hatırlanması gereken bir yazma kontrolü olmaktan çıkar; `ITenantOwned` implemente eden her entity otomatik olarak bu kontrole tabidir. (Okuma yolu için bu geçerli değildir — bkz. yukarıdaki §4 notu; global bir query filter eklenene kadar okuma tarafındaki tenant filtresi repository sorgularının kendi sorumluluğundadır.)
- Çok kulüplü üyelik modeli (bir kullanıcının birden fazla kulüpte farklı rolleri) doğal olarak desteklenir; token'ın yeniden verilmesini gerektirmeden aktif kulüp istek bazında değişebilir.
- Platform Admin bypass'larının zorunlu audit'e tabi olması, ayrıcalıklı erişimin kötüye kullanımını caydırır ve iz bırakır.

**Olumsuz / riskler:**

- Her istekte ek bir "aktif üyelik doğrulama" sorgusu/lookup gerekir; bu, dikkatli cache'lenmezse (ör. kısa ömürlü Redis cache) ek gecikme getirebilir.
- Header tabanlı yaklaşım, istemci tarafının her istekte doğru `X-Club-Id`'yi göndermesini gerektirir; istemci hatası (yanlış/eksik header) kullanıcı deneyiminde `400`/`403` hatalarına yol açabilir — API dokümantasyonunda bu header açıkça zorunlu olarak işaretlenmelidir.
- `ITenantOwned` işaretlemesi geliştiricinin sorumluluğundadır: platform-geneli bir aggregate'e yanlışlıkla `ITenantOwned` eklemek (veya tenant'a bağlı bir aggregate'e eklemeyi unutmak) mimari testlerle sürekli izlenmelidir; `SaveChangesAsync` yalnızca `ITenantOwned` işaretli entity'leri kontrol eder, işaretlenmemiş bir aggregate sessizce kontrolsüz kalır.
- Global bir EF Core query filter'ı henüz eklenmediğinden, tenant izolasyonu şu an yalnızca yazma yolunda (`SaveChangesAsync`) merkezi olarak zorlanır; okuma yolunda bir repository metodunun tenant filtresi eklemeyi unutması, veriyi başka bir tenant'a sızdırabilir — bu risk şu an repository kod inceleme disiplinine ve `SecurityTests`'e dayanır, yapısal olarak imkansız kılınmamıştır.

## İlgili

- [ARCHITECTURE.md §7](../ARCHITECTURE.md#7-tenant-çözümleme-akışı)
- [SECURITY.md §6](../SECURITY.md#6-threat-model-temel) — tenant ID manipülasyonu tehdidi
- Ana geliştirme talimatı §7
