---
name: ui-design-system
description: Frontend'de görsel iş yaparken kullanılır. Material UI tabanlı FatureBase tasarım sistemi - tema, bileşen köprüleri, ikon kuralları ve profesyonel görünüm ilkeleri.
---

# UI Design System (Material UI)

Arayüz **MUI v9** üzerine kuruludur; tema `src/theme.ts`'tedir (birincil: derin deniz mavisi
`#155e75`, vurgu: turuncu `#ea580c`, zemin `#f4f6f8`, radius 10, Roboto). Yeni ekran yaparken
MUI şablonlarındaki (dashboard/sign-in) desenleri temel al.

## Bileşen katmanı (sayfalar BUNLARI kullanır, çıplak MUI'yi değil)

`components/` altındaki sarmalayıcılar sayfa sözleşmesini sabitler - MUI'ye buradan köprülenir:

- `Button` (variant: primary/brick/ghost/danger; loading spinner'lı) → MuiButton
- `Field` (label+value/onChange → TextField; children verilirse etiket + native kontrol)
- `Notice` → Alert; `Tag` → Chip; `Skeleton` → MuiSkeleton; `Switch` → MuiSwitch
- `Empty` (ikonlu boş durum), `Form`, `useAction` (başarı=toast, hata=inline Alert)
- `Modal` → Dialog (mobilde fullScreen); `useConfirm` → promise'li onay Dialog'u (`window.confirm` YASAK)
- `toast.tsx` → yığılan MUI Alert bildirimleri; `PanelLayout` → AppBar + responsive Drawer
  (MUI dashboard şablonu deseni); `OtpInput` → 6 kutulu TextField.

## İkonlar

YALNIZCA `@mui/icons-material` (Rounded varyantlar tercih). Sayfalar `components/icons.tsx`
köprüsündeki `Icon*` adlarını kullanır; yeni ikon gerekirse köprüye ekle, sayfaya doğrudan
`@mui/icons-material` importu serpiştirme.

## CSS yardımcıları (styles.css)

MUI dışı kalan yapılar için sınıflar Material estetiğine hizalıdır: `.card`, `table.list`
(hover'lı satırlar), `.slots/.slot`, `.stat-row/.stat`, `.tabs`, `.info-line`, `.site-hero`,
`.landing-*`, `.skel`, ham `.tag`/`.btn`. Native `<select>/<input>/<textarea>` otomatik outlined
görünür - MUI Select'e çevirmek şart değil.

## İlkeler

1. Renk/spacing için tema token'larını (`sx={{ color: "text.secondary" }}`) veya CSS
   değişkenlerini kullan; ham hex serpiştirme.
2. Yükleme = `Skeleton`; yıkıcı işlem = `useConfirm({danger:true})`; başarı = toast.
3. Metinler Türkçe, alana özgü ve somut; boş durumlar yol gösterir.
4. Mobil öncelikli: Drawer mobilde çekmece, Dialog mobilde tam ekran, `.grid2` tek sütuna düşer,
   tablolar `.table-wrap` ile yatay kayar.
5. Paneller `React.lazy` ile bölünür; ağır medya listede değil tıklamayla yüklenir.
