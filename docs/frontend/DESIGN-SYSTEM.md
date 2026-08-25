# CrewBase — Design System

## Direction

Premium rowing-club operations platform. Light-first, calm, editorial-sport.
**Not:** dark SaaS dashboard, neon gradients, glassmorphism, crypto/analytics aesthetics.

References in spirit: rowing heritage (oar blades, water lines, boathouse wood), Turkish
sports-club administration clarity. Generous whitespace, strong typographic hierarchy,
restrained accent usage, real data density for operations screens.

## Tokens (CSS custom properties, Tailwind v4 theme mapping)

```text
Color
  --color-canvas        #F7F8FA   app background (cool paper)
  --color-surface       #FFFFFF   cards, panels
  --color-surface-2     #EFF3F6   subtle wells, table headers
  --color-line          #DDE4EA   borders/dividers
  --color-ink           #16232E   primary text (deep navy-charcoal)
  --color-ink-2         #51626F   secondary text
  --color-ink-3         #8595A1   muted/meta
  --color-brand-*       deep water blue scale (50..900), base #145C86 — buttons/links/focus
  --color-oar-*         warm cedar accent (#B4632F) — sparing highlights, active nav marker
  --color-success/warning/danger/info  semantic scales (AA on surface)
  status palette: Pending amber / Confirmed blue / Completed green / Cancelled grey-red

Typography
  Display: "Sora" or "Manrope" 600/700 (page titles, hero numerals)
  Body/UI: "Inter" 400/500/600 (Turkish glyph-complete)
  Mono: "JetBrains Mono" (times HH:mm, codes KRK-XXXXXX, memberCode)
  Scale: 12/13/14(base)/16/18/22/28/36; line-height 1.5 body, 1.15 display

Spacing    4-point grid: 4,8,12,16,24,32,48,64
Radius     sm 6 / md 10 / lg 14 / pill 999 (cards lg, inputs md, chips pill)
Shadow     xs hairline card / md raised dialog-drawer (soft, low-alpha; no glow)
Motion     fast 120ms, base 180ms ease-out; respects prefers-reduced-motion
Breakpoints sm 640 / md 768 / lg 1024 / xl 1280
Z-index     dropdown 1000 · drawer 1100 · modal 1200 · toast 1300
```

## Components (build only what features need)

Primitives: Button (primary/secondary/ghost/danger + pending state), Input, Textarea,
Select, Checkbox, Radio, Switch, DatePicker (native-input based wrapper), Badge/Chip,
Tabs, Tooltip, Toast, Dialog, Drawer, Skeleton, Table primitives.

Patterns: PageHeader (title/context/actions), FilterBar (date scope, selects, search),
AsyncBoundary (loading/error/empty states incl. permission-denied & not-found variants),
StatCard, CapacityMeter (seats x/y), StatusBadge (domain enums), EmptyState, ErrorState,
ConfirmDialog (destructive flows), FormField (label+control+error aria-wired),
SectionCard, DayPicker strip (date scoping), TimelineRow (activity logs).

Accessibility baseline: labelled controls, `aria-invalid`+`aria-describedby` errors,
focus-visible rings (brand), Dialog/Drawer focus trap + return focus, Esc close,
semantic tables with caption, contrast ≥4.5:1, keyboard paths for all actions,
`prefers-reduced-motion`.

Responsive: desktop-first operations tables → md/lg grids; mobile switches to stacked cards
(members/appointments), bottom-sheet dialogs on <md, sticky date strip.

## Labels (i18n-ready)

All user-facing domain strings centralized in `packages/design-system/src/labels/tr.ts`:
appointment statuses, boat classes w/ capacity, rowing levels (11 exact backend labels),
member log event names (`MemberEvents` + consent/log extras), card types/statuses,
friend directions/statuses, media kinds, consent titles come from API data (never hardcoded).
Components never inline these strings; future i18n = swap label module, not rewrite UI.
