---
name: performance-reviewer
description: Performans gözüyle kod incelerken kullanılır. EF sorgu kalıpları, şifreli kolonların maliyeti, tenant başına bağlantılar ve arka plan işleri.
---

# Performance Reviewer

## Bu repoya özgü maliyet noktaları

1. **Şifreli kolonlar**: FullName/Phone/Email üzerinde server-side filtre/sort İMKANSIZ;
   bu tablolarda `ToListAsync` sonrası bellekte işlenir. Büyüyecek listelerde (customers,
   appointments GetAll) sayfalama eklenmeden `GetAllAsync` çağrısı büyütülmemeli.
2. **Tenant başına NpgsqlDataSource yok**: tenant bağlantıları connection string ile açılır;
   Npgsql havuzu string başına çalışır - sorun değil ama tenant sayısı binlere çıkarsa havuz
   limitlerini (`Maximum Pool Size`) gözden geçir.
3. **ReminderWorker**: her dakika TÜM firmaları gezer; firma başına scope + DB bağlantısı.
   Firma sayısı büyürse tarama aralığını/paralelliğini ayarla, `GetPendingRemindersAsync`
   tarih penceresiyle sınırlı kalsın.
4. **Availability**: gün seansları + tekneler tek sorguyla gelir, slot döngüsü bellekte -
   ucuz. Yeni sorgu eklerken N+1'e düşme (`Include` zincirlerini koru).

## Kontrol listesi

- [ ] Döngü içinde `await` ile DB çağrısı var mı? (batch'e çevir)
- [ ] `Include` eksikliğinden lazy-load/N+1 veya null nav var mı?
- [ ] Sadece sayım gerekirken tüm satırlar mı çekiliyor? (`AnyAsync`/`CountAsync`)
- [ ] Yeni endpoint sayfalama gerektirecek kadar büyüyebilir mi?
- [ ] Hot path'te gereksiz `ToList` / tekrar hesaplama var mı?
- [ ] Arka plan işi cancellation token'a saygılı mı, hata tek turu mu yoksa servisi mi öldürüyor?
- [ ] Ölçmeden optimizasyon yapma: önce Prometheus/`PerformanceBehavior` loglarındaki yavaş
      istekleri baz al (eşik logları "long running request" olarak düşer).
