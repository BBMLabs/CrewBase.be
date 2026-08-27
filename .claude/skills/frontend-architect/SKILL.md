---
name: frontend-architect
description: frontend/ (React + Vite) tarafında sayfa/akış eklerken kullanılır. Subdomain tabanlı yönlendirme, panel yapısı ve API istemci desenleri.
---

# Frontend Architect

`frontend/` Vite + React 18 + TypeScript + react-router. Tailwind YOK - tek `src/styles.css`
tasarım sistemi. Backend: `http://localhost:5283` (VITE_API_URL ile değiştirilebilir), CORS açık.

## Alan adı mimarisi (kritik)

`App.tsx` yönlendirmeyi hostname'e göre ikiye ayırır:

- **`{firma}.faturebase.com`** (dev: `{firma}.localhost:5173`) → firma sitesi (`tenant/TenantSite`)
  ve üye paneli (`member/MemberPanel`, `/uye/*`). Subdomain `lib/tenant.ts#currentSubdomain` ile
  bulunur; API çağrıları path-tabanlıdır: `/api/v1/public/{subdomain}/...`.
- **kök alan adı** → `platform/Landing` (firma kaydı), `/giris` (firma+master girişi, rol JWT'den
  çözülür), `/panel/*` (firma paneli), `/master/*` (master paneli).

Firma sitesine link verirken ASLA `/site/{sub}` yolu kullanma; `lib/tenant.ts#siteUrlFor`
(gerçek subdomain URL'i) ve görünüm için `displaySiteUrl` kullan.

## Yapı

```
src/lib        api.ts (fetch + oturum), tenant.ts, format.ts (TR tarih/etiketler)
src/components ui.tsx (Field/Form/Notice/Tag/useAction), PanelLayout.tsx (responsive sidebar)
src/tenant     public site + BookingWidget (misafir/üye ortak rezervasyon)
src/member     üye paneli
src/company    firma paneli (pages/ altında ekran başına dosya)
src/master     platform paneli
src/platform   landing + login
```

## Kurallar

1. Yeni panel ekranı = `company/pages/X.tsx` + `CompanyPanel.tsx`'te nav + route satırı.
2. Veri çekme deseni: `useState<T|null>` + `useEffect(load)` + `load()` yenilemesi;
   mutasyonlar `useAction().run(fn, okMesaj)` ile (hata/success otomatik).
3. API tipi backend DTO'suyla birebir (camelCase); zarfı `api.ts` açar, sayfalar sade `T` görür.
4. Rol korumaları `RequireRole` ile; ayrıca backend zaten her isteği doğrular - UI koruması
   yalnızca kullanıcı deneyimi içindir.
5. Metinler Türkçe ve gerçek ürün diliyle yazılır; İngilizce placeholder bırakma.
