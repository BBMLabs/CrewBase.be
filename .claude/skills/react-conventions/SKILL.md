---
name: react-conventions
description: frontend/ içinde React kodu yazarken uygulanan desenler - veri çekme, form/mutasyon, oturum, tip disiplini.
---

# React Conventions

## Veri çekme

```tsx
const [items, setItems] = useState<T[] | null>(null); // null = yükleniyor
function load() { api.get<T[]>("/api/v1/...").then(setItems).catch(() => setItems([])); }
useEffect(load, [bağımlılıklar]);
```
Render üç durumu ayırt eder: `null` → "Yükleniyor…", boş dizi → `<Empty text="..."/>`, dolu → liste.

## Mutasyon

`useAction()` kancası tek yerden busy/hata/başarı yönetir; başarı mesajı otomatik TOAST olur,
hata forma yakın inline gösterilir:

```tsx
const action = useAction();
action.run(async () => { await api.post(...); load(); }, "Kaydedildi.");   // -> toast
// <Notice kind="err" text={action.err} />  (yalnızca hata inline)
// Gönderen buton: <Button loading={action.busy}>Kaydet</Button>
```

Yıkıcı işlemler `useConfirm()` ile onaylanır (`window.confirm` kullanılmaz):

```tsx
const confirmDialog = useConfirm();
const yes = await confirmDialog({ title: "...", text: "...", confirmLabel: "Sil", danger: true });
```

Form diyalogları `Modal` bileşeniyle açılır (Members/Packages'taki Add*Modal deseni).
Paneller `App.tsx`'te `React.lazy` ile bölünmüştür; yeni üst seviye sayfayı da lazy ekle.

## API istemcisi

- `api.get/post/put/del<T>` zarfı açar (`data` döner); hata `ApiError(status, code, message)`.
- Anonim uçlarda üçüncü parametre `false` (auth header gönderme).
- 401'de oturum temizlenip sayfa yenilenir - sayfalarda 401 işleme yazma.

## Oturum

- `saveSession/getSession/clearSession` (sessionStorage, tek anahtar). Rol: `company | member | master`.
- Route koruması `App.tsx#RequireRole`; rol JWT claim'inden bir kez `LoginPage`'de çözülür.
- PII'yi (ad/telefon/e-posta) state dışında hiçbir yere yazma - localStorage/sessionStorage'a asla.

## Stil disiplini

- Bileşenler function declaration; props inline tip. Global state kütüphanesi yok - ihtiyaç
  local state + prop'la çözülür.
- Tarih ISO string (`yyyy-MM-dd`) taşınır; gösterim `fmtDate/fmtDateTime`, durum `Tag`,
  Türkçe eşlemeler `format.ts`'te (`STATUS_TR`, `EVENT_TR`) - sayfa içinde sözlük kurma.
- `tsc -b` temiz kalmalı; `any` yalnızca lib sınırında.
