# 0002 — MongoDB Tek Veritabanı Stratejisi

> **SUPERSEDED.** Bu ADR **artık geçerli değildir**; yerini [adr/0004-postgresql-tek-veritabani-stratejisi.md](./0004-postgresql-tek-veritabani-stratejisi.md) almıştır — platform, MongoDB'den PostgreSQL + EF Core'a taşınmıştır. Bu dosya, o dönemdeki kararın gerekçesini ve bağlamını **tarihsel kayıt olarak** korumak amacıyla değiştirilmeden bırakılmıştır; aşağıdaki içerik güncel mimariyi yansıtmaz.

## Durum

~~Kabul edildi.~~ **Superseded** (bkz. yukarıdaki not). **Bu ADR, önceki "0002 — Oracle ve MongoDB'nin Birlikte Kullanımı" kararının yerine geçmişti (supersedes)** — Oracle+MongoDB'yi birlikte kullanma kararı tamamen iptal edilmiştir; bu dosya aynı ADR numarasını (0002) taşımaya devam eder, eski dosya kaldırılmıştır.

## Bağlam

Ana geliştirme talimatı (`kurek-kulubu-claude-code-gelistirme-talimatlari.md`), RowingClub'ın veri katmanı için ikili bir strateji öngörüyordu: transaction bütünlüğü gereken, tenant'a bağlı, ilişkisel veri için **Oracle**; esnek/doküman yapılı, sık şeması değişebilen veri için **MongoDB** (bkz. eski `0002-oracle-ve-mongodb-birlikte-kullanim.md`). Bu karar, "her veri kendi doğasına en uygun depoda tutulmalı" prensibine dayanıyordu.

Identity modülünün implementasyonu ilerlerken ürün sahibi bu spesifikasyonu **açıkça değiştirdi**: *"yapıda oracle db olmayacak sadece mongo db"* — mimaride Oracle bulunmayacak, yalnızca MongoDB kullanılacak. Bunu, MongoDB'nin karşılaması gereken somut yeteneklerin listesi izledi: birden fazla aggregate/koleksiyonu kapsayan atomik yazımlar (transaction), optimistic concurrency, versiyonlu şema/migration yönetimi, outbox deseninin atomicity garantisi.

Bu, orijinal kararın gerekçesini (ilişkisel veri için Oracle, esnek veri için MongoDB) geçersiz kılan bir ürün kararıdır — teknik bir yeniden değerlendirme değil, doğrudan bir talimat değişikliğidir. Bu ADR, önceki kararın yerini alan yeni kararı ve bunun pratikte nasıl karşılandığını (MongoDB'nin transaction/concurrency/migration ihtiyaçlarını nasıl karşıladığını) dokümante eder.

## Karar

RowingClub, **tek veri deposu olarak yalnızca MongoDB** kullanır. Oracle veya başka bir ilişkisel veritabanı mimaride yer almaz.

Somut kararlar:

1. **Tek mantıksal veritabanı**: `Mongo:DatabaseName` (`MONGODB_DATABASE_NAME`, varsayılan `rowingclub`). Tüm modüller aynı veritabanına, `<modül>_<aggregate>` adlandırma kuralıyla ayrılmış koleksiyonlara yazar (ör. `identity_users`). Bu, modüller arası bir handler'ın (ör. randevu + kontenjan + paket hakkı) birden fazla koleksiyondan aggregate okuyup tek bir transaction'da yazabilmesini, cross-database transaction karmaşıklığı olmadan mümkün kılar.
2. **Replica set zorunlu, transaction bunun üzerine kurulu**: Outbox deseninin atomicity garantisi (aggregate yazımı + domain event'in outbox'a yazımı **aynı anda ya hep ya hiç**), MongoDB'nin multi-document transaction'ları ile sağlanır; bu özellik yalnızca bir replica set'e karşı çalışır (standalone `mongod` desteklemez). Bağlantı stringi `replicaSet=rs0` içermek zorundadır; `docker-compose.yml`'deki `mongodb` servisi `--replSet rs0` ile başlar ve healthcheck'i ilk açılışta tek node'luk replica set'i idempotent şekilde initiate eder.
3. **Whole-document optimistic concurrency**: Oracle'ın satır bazlı `RowVersion`/concurrency token'ının karşılığı olarak, her `AggregateRoot<TId>`'de bir `Version` (long) alanı bulunur. `MongoUnitOfWork.SaveChangesAsync`, her izlenen aggregate'i `{_id, version}` filtresiyle whole-document `ReplaceOneAsync` ile yazar; filtre eşleşmezse (başka bir writer araya girmiştir) `ConcurrencyException` fırlatılır (→ HTTP 409). Ayrı bir pes-simist kilitleme mekanizması yoktur.
4. **Versiyonlu migration + şema yönetimi**: EF Core migration'larının karşılığı olarak `IMongoMigration` (Version, Name, ExecuteAsync) implementasyonları, uygulama başlangıcında `MongoMigrationHostedService` tarafından otomatik ve idempotent şekilde çalıştırılır, `schema_migrations` koleksiyonunda izlenir. Her migration, ilgili koleksiyonu (yoksa) bir `$jsonSchema` validator'ıyla (yalnızca zorunlu alanlar) oluşturur ve indekslerini kurar — MongoDB'nin şemasız doğasına rağmen asgari bir yapısal disiplin korunur. Modül başına versiyon aralığı ayrılmıştır (BuildingBlocks 1-99, Identity 100-199, ... bkz. [DATA_MODEL.md](../DATA_MODEL.md)).
5. **Outbox/Inbox Mongo-native**: `OutboxMessage` artık EF konfigürasyonu olmadan, doğrudan Mongo dokümanı olarak yazılır ve aggregate'i tetikleyen domain event ile **aynı transaction'da** yazılır — bu, iki farklı veritabanı (Oracle + Mongo) arasında distributed transaction gerektiren eski tasarımın ortadan kalkmasının doğrudan sonucudur. `InboxMessage`, ileride yazılacak idempotent consumer'ların kullanacağı şekli tanımlar.
6. **Tenant izolasyonu Mongo-native**: `MongoUnitOfWork.Track()`, `ITenantOwned` aggregate'leri izlemeye alındığı anda `ICurrentTenant`'a karşı doğrular — EF Core global query filter + `SaveChanges` interceptor ikilisinin tek bir mekanizmadaki karşılığı (bkz. [adr/0003-tenant-izolasyon-stratejisi.md](./0003-tenant-izolasyon-stratejisi.md)).

## Sonuçlar

**Olumlu:**

- Tek veritabanı teknolojisi: operasyonel yük (yedekleme, izleme, ölçeklendirme, personel uzmanlığı, tek Testcontainers kurulumu) önemli ölçüde azalır; eski tasarımdaki Oracle↔MongoDB eventual consistency karmaşıklığı tamamen ortadan kalkar (artık outbox yazımı ile aggregate yazımı **aynı** transaction'dadır, ayrı bir "iki depo arasında senkronize olma" problemi yok).
- Şema esnekliği: yeni bir alan eklemek var olan dokümanları etkilemez; `$jsonSchema`'nın yalnızca zorunlu alanları kilitleyen, katı olmayan tasarımı geliştirme hızını korur.
- Tek `IMongoUnitOfWork` üzerinden, modüller arası aggregate'leri kapsayan atomik transaction'lar (ör. randevu + kontenjan + paket hakkı) özel bir mekanizma gerekmeden desteklenir — dağıtık transaction karmaşıklığı hiç gündeme gelmez.

**Olumsuz / riskler:**

- Oracle'ın olgun ilişkisel araç ekosistemi (deklaratif foreign key zorlaması, karmaşık join'ler, mature migration tooling, uzun yıllara dayanan operasyonel deneyim) kaybedilir; bunun yerine MongoDB'nin doküman modeli ve uygulama seviyesinde zorlanan referans bütünlüğüyle çalışılır. `$jsonSchema` validator'ları ve versiyonlu migration'lar, bu boşluğu tamamen kapatmasa da asgari bir disiplin sağlayarak riski azaltır.
- Whole-document replace, çok büyük/sık güncellenen aggregate'lerde (henüz gözlemlenmedi) yazma maliyetini ilişkisel bir "yalnızca değişen sütunları güncelle" yaklaşımına göre artırabilir; aggregate'lerin makul boyutta kalması (DDD aggregate tasarım disiplini) bunu sınırlar.
- MongoDB.Driver için henüz stabil bir resmi OpenTelemetry instrumentation paketi yok; Mongo sorgu span'leri şu an izlenmiyor (bkz. [OBSERVABILITY.md](../OBSERVABILITY.md)).
- Foreign key benzeri bütünlük kısıtları (ör. bir `RefreshToken.userId`'nin gerçekten var olan bir `User`'a işaret etmesi) veritabanı seviyesinde zorlanmaz; bu bütünlük yalnızca uygulama kodunda (handler/domain seviyesinde) sağlanır.

## İlgili

- [DATA_MODEL.md](../DATA_MODEL.md) — koleksiyon/indeks/şema detayları
- [ARCHITECTURE.md §8-9](../ARCHITECTURE.md#8-outboxinbox-akışı) — outbox akışı ve MongoDB veri modeli
- [adr/0003-tenant-izolasyon-stratejisi.md](./0003-tenant-izolasyon-stratejisi.md) — tenant izolasyonunun Mongo-native karşılığı
- `src/BuildingBlocks/RowingClub.BuildingBlocks.Infrastructure/Mongo/` — `MongoUnitOfWork`, migration altyapısı
