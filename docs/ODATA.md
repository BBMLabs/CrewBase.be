# OData Güvenliği

## Durum

OData yalnızca **read-only raporlama ve listeleme** amacıyla kullanılır; hiçbir yazma işlemi OData üzerinden yapılmaz. Talimat §13'te öngörülen OData endpoint'leri (`/odata/v1/clubs/{clubId}/lesson-sessions`, `/odata/v1/clubs/{clubId}/members`) **Scheduling** ve **Memberships** modüllerine bağlı olduğundan, bu modüller henüz implemente edilmediği için **OData endpoint'leri şu anda dışa açılmamıştır**. Bu doküman, o endpoint'ler eklendiğinde uygulanacak zorunlu güvenlik kurallarını önceden tanımlar; implementasyon bu kurallara göre yapılacaktır.

## Planlanan Endpoint'ler

| Route | Modül | Amaç |
|---|---|---|
| `/odata/v1/clubs/{clubId}/lesson-sessions` | Scheduling | Ders seanslarının kontrollü listelenmesi/raporlanması |
| `/odata/v1/clubs/{clubId}/members` | Memberships | Üye listesinin kontrollü listelenmesi/raporlanması |

Reporting modülü de kendi ihtiyaçları için ek OData yüzeyleri tanımlayabilir; bu durumda aynı kurallar geçerlidir.

## Zorunlu Kurallar

1. **Her entity OData'ya açılmaz.** Yalnızca açıkça listelenen, raporlama amaçlı entity'ler OData yüzeyine (genelde ayrı bir read-model/DTO projeksiyonu olarak) açılır — domain entity'lerinin kendisi değil.
2. **Query option allow-list**: `$select`, `$filter`, `$orderby`, `$top`, `$skip` yalnızca önceden tanımlanmış, izin verilen alan/işlem kümesiyle sınırlıdır. Allow-list dışındaki alanlarda filtre/sıralama reddedilir.
3. **`$expand` varsayılan olarak kapalıdır.** İhtiyaç halinde yalnızca açıkça izin verilen, sığ (tek seviye) ilişkiler için etkinleştirilebilir; sınırsız/derin `$expand` asla açılmaz.
4. **Maksimum `$top` değeri** her endpoint için sabit bir üst sınırla tanımlanır (ör. varsayılan sayfa boyutu 50, maksimum 200 gibi somut değerler ilgili modül implementasyonunda netleştirilecek ve burada güncellenecektir); istemci bu sınırı aşamaz.
5. **Sorgu karmaşıklığı sınırlandırılır**: iç içe `$filter` ifadeleri, çoklu `$orderby`, büyük `$expand` zincirleri gibi performansı tehdit eden kombinasyonlar reddedilir veya sabit bir karmaşıklık bütçesiyle sınırlanır.
6. **Tenant filtresi asla atlanamaz.** Route'daki `{clubId}` ile `X-Club-Id` header'ı ve current tenant context'i eşleşmeli; OData sorgu motoru, alttaki repository/queryable'a tenant filtresi uygulanmış halde erişir — istemci `$filter` ile bu filtreyi ezemez veya bypass edemez.
7. **Hassas alanlar DTO'ya dahil edilmez.** Alan bazlı şifrelenen veriler (telefon, adres, sağlık beyanı vb.) ve iç sistem alanları (ör. hashlenmiş credential, internal id) OData projeksiyonunda yer almaz.
8. **Query timeout uygulanır.** Uzun süren sorgular sunucu tarafında zaman aşımına uğratılır ve `504`/uygun Problem Details ile sonlandırılır.
9. **OData endpoint'leri ayrıca policy ile korunur.** Normal REST endpoint'leri gibi authentication + tenant + rol/permission kontrolüne tabidir (ör. `CanViewClubReports`); OData olması authorization'ı atlatmaz.
10. **OData sorguları log ve metric üretir.** Her OData isteği için endpoint, uygulanan query option'lar (ham değerler değil, normalize edilmiş/loglanabilir formda), süre ve sonuç sayısı loglanır/metriklenir; anormal sorgu paternleri (ör. tekrarlanan `$top` maksimum denemeleri) izlenebilir olmalıdır.

## Uygulama Notları

- OData Microsoft.AspNetCore.OData paketi ile Minimal API route group'larına entegre edilecektir.
- Her OData endpoint'i için allow-listed alan seti ve maksimum `$top` değeri, ilgili modülün `Contracts`/`Api` katmanında merkezi bir konfigürasyon olarak tanımlanmalı, kod içine dağınık sabit olarak yazılmamalıdır.
- OData üzerinden dönen veri kümesi her zaman tenant'a filtrelenmiş bir `IQueryable` üzerinden başlamalı; asla tüm tenant'ların verisini içeren bir küme üzerinden `$filter` ile daraltma yapılmamalıdır (performans ve güvenlik riski).

## İlişkili Dokümanlar

- Tenant filtresi mekanizması: [ARCHITECTURE.md §7](./ARCHITECTURE.md#7-tenant-çözümleme-akışı)
- Rol/policy modeli: [SECURITY.md §2](./SECURITY.md#2-authorization-modeli)
- Endpoint bazlı detaylar (eklendiğinde): `docs/API_ENDPOINTS.md`, `docs/AUTHORIZATION_MATRIX.md`
