---
name: feature-planner
description: Yeni bir özellik isteğini bu repoya uygun adımlara bölerken kullanılır. Katman sırası, migration ihtiyacı, test ve doğrulama planı çıkarır.
---

# Feature Planner

Bir özelliği uygulamaya başlamadan önce şu planı çıkar ve sırayla uygula:

1. **Alan analizi**: Özellik hangi modüle ait (Identity / Scheduling / yeni modül)? Veri katalog
   DB'sinde mi (firma/kullanıcı) tenant DB'sinde mi (üye/randevu/kaynak)?
2. **Domain**: Entity/değer kuralları + repository arayüzü. İş kuralı ihlalleri
   `DomainException(code, türkçe mesaj)`.
3. **Application**: Command/Query + Handler (+Validator). Yazma → `ICommand<T>`;
   tenant yazması → handler sonunda `ISchedulingUnitOfWork.SaveChangesAsync`.
4. **Infrastructure**: Repository implementasyonu, EF map'i, DI kaydı. Şema değiştiyse migration
   (hangi context: `RowingClubDbContext` katalog / `TenantDbContext` tenant).
5. **API**: Endpoint (public → subdomain'den tenant çözümü, panel → CompanyId'den; rol denetimi
   `RequireAuthorization`), request record, `ApiResponse<T>` yanıt.
6. **Kırılanları güncelle**: ctor imzası değişen handler'ların unit testleri; endpoint listesi
   değiştiyse Functional/Security testlerdeki endpoint envanterleri.
7. **Doğrulama**: `dotnet build` → `dotnet test` → API'yi çalıştırıp curl ile uçtan uca akış
   (kayıt → site → rezervasyon → panel gibi).

Plan çıktısı kısa olmalı: dosya listesi + her adımda ne değişecek. Büyük özellikte önce dikey
dilim (en ince uçtan uca akış), sonra genişletme.
