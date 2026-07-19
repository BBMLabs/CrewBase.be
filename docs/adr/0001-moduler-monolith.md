# 0001 — Modüler Monolith Mimarisi

## Durum

Kabul edildi.

## Bağlam

RowingClub, kürek kulüplerinin üyelerini, eğitmenlerini, derslerini, randevularını, ders paketlerini ve kulüp operasyonlarını yönettiği çok kiracılı bir platformdur. Yedi bounded context tanımlanmıştır: Identity, Clubs, Memberships, Scheduling, Packages, Notifications, Reporting. Bu contextler birbiriyle ilişkilidir (ör. bir randevu, bir üyeliğe ve bir ders seansına bağlıdır) ve platformun erken aşamasında ekip küçüktür, ürün-pazar uyumu henüz netleşmemiştir.

İki temel seçenek değerlendirildi:

1. **Mikroservisler**: Her bounded context bağımsız bir servis, bağımsız bir veritabanı ve bağımsız bir deployment pipeline'ı ile.
2. **Modüler Monolith**: Tek bir deployment biriminde, net katman ve modül sınırlarına sahip, DDD tabanlı bir yapı.

## Karar

RowingClub, **modüler monolith** olarak inşa edilecektir. Modüller (`Identity`, `Clubs`, `Memberships`, `Scheduling`, `Packages`, `Notifications`, `Reporting`) her biri kendi `Domain`/`Application`/`Infrastructure`/`Contracts` projelerine sahip, net sınırları olan, ileride bağımsız servislere ayrılabilecek şekilde tasarlanır.

Uygulanan kurallar:

- Domain katmanı hiçbir dış bağımlılığa sahip değildir; Infrastructure veya API'ye bağımlı olamaz.
- Modüller birbirinin `Domain`/`Infrastructure` katmanına doğrudan referans veremez — yalnızca `Contracts` projeleri (paylaşılan sözleşmeler) ve entegrasyon event'leri üzerinden iletişim kurar.
- `RowingClub.Api` yalnızca `RowingClub.Bootstrapper`'a bağımlıdır; hiçbir modülün Infrastructure katmanına doğrudan dokunmaz. Tüm modül wiring'i `Bootstrapper` üzerinden yapılır.
- Bu sınırlar `tests/RowingClub.ArchitectureTests` içinde NetArchTest ile otomatik doğrulanır.

Mikroservislere geçiş, bu sınırlar zaten net olduğu için ileride mümkün olacak şekilde bırakılmıştır; ancak şu an için tercih edilen dağıtım birimi tektir.

## Sonuçlar

**Olumlu:**

- Tek deployment birimi; erken aşamada operasyonel karmaşıklık (servis keşfi, dağıtık transaction, network güvenilirliği, ayrı CI/CD hatları) önemli ölçüde azalır.
- Modüller arası tutarlılık (ör. randevu oluştururken üyelik ve paket kontrolü) süreç içi çağrılarla, dağıtık transaction'a ihtiyaç duymadan sağlanabilir.
- DDD sınırları netleştirilmiş olduğu için, ihtiyaç doğduğunda bir modülü bağımsız bir servise çıkarmak (extract) görece düşük maliyetli olacaktır.
- Tek codebase, tek build/test pipeline'ı; geliştirme hızını artırır.

**Olumsuz / riskler:**

- Modül sınırlarına disiplinli uyulmazsa (ör. bir modülün başka bir modülün Domain'ine gizlice bağımlı hale gelmesi) "big ball of mud" riski oluşur — bu risk `ArchitectureTests` ile azaltılmıştır.
- Tüm modüller aynı process'te çalıştığından, bir modüldeki ağır kaynak tüketimi (ör. büyük bir raporlama sorgusu) diğer modülleri de etkileyebilir; bağımsız ölçekleme mümkün değildir (yalnızca tüm `api` instance'ı birlikte ölçeklenir).
- Tek veritabanı bağlantı havuzu ve tek deployment penceresi, modüller arası bağımsız release ritmini engeller.

## İlgili

- [ARCHITECTURE.md](../ARCHITECTURE.md) — katman ve modül sınırları
- Ana geliştirme talimatı §3, §4, §5
