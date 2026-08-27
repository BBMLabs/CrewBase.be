---
name: code-reviewer
description: Genel kod incelemesinde kullanılır. Bu reponun stil/desen sözleşmeleri ve inceleme öncelik sırası.
---

# Code Reviewer

Önce doğruluk, sonra güvenlik (security-reviewer skill'i), sonra sadelik/tutarlılık.
Bulguları önem sırasıyla, dosya:satır ve somut senaryo ile ver.

## Doğruluk

- Domain kuralı handler'a mı sızmış, entity'de mi? (kural entity'de olmalı)
- `ICommand<T>` mi `IRequest<T>` mi doğru seçilmiş? (yazma = ICommand; tenant yazması ayrıca
  `ISchedulingUnitOfWork.SaveChangesAsync` çağırmalı - unutulursa değişiklik kaybolur!)
- Null/boş durumlar: `?? CompanySettings.Default()`, `FirstOrDefault` sonrası null kontrolü.
- DateOnly/TimeOnly/DateTimeOffset karışımı: yerel saat hesapları `settings.NowLocal()` ile.

## Tutarlılık sözleşmeleri

- Hata: `DomainException("snake_code", "Türkçe kullanıcı mesajı")`; endpoint'te erken dönüş
  `ApiResponse.Fail`. Yeni hata kodu eklemeden önce sözlükte var mı bak.
- İsimlendirme: `*CommandHandler`, `*QueryHandler`, `*Repository`, `*Dto`, `*AtUtc`.
- Yorumlar: koddan okunamayan kısıt/niyet için, Türkçe; "ne yaptığını anlatan" yorum ekleme.
- XML doc yalnızca sınıf seviyesinde ve mimari bağlam veriyorsa.
- Public API yüzeyinde Domain entity'si dönme; DTO map'le.

## Sadelik

- Aynı resolve/doğrulama üç kez tekrarlanıyorsa helper'a çek (endpoint'lerdeki
  `ResolveOwnCompanyAsync` gibi).
- Kullanılmayan using/param/record bırakma.
- Erken soyutlama ekleme: ikinci kullanıcısı olmayan interface şüphelidir (istisna:
  katman kuralı gereği repo arayüzleri).

## Kapanış kontrolü

`dotnet build` temiz + `dotnet test` yeşil + değişen endpoint'ler test envanterlerine yansımış.
