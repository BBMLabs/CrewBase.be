# CrewBase — UI/UX Audit

> Date: 2026-08-22 · Scope: all three apps + design system.
> Method: code inspection against verified backend contract (`API-INVENTORY.md`).
> Verdict scale: ✅ solid · 🟡 needs polish · 🔴 needs redesign.

## Company Panel

| Screen | Verdict | Findings |
|---|---|---|
| Login/Register | ✅ | Split-screen brand rail with water scene, staggered entrance, Turkish rowing copy. Validation mirrors backend validators. |
| Dashboard | ✅ | Answers "Bugün kulüpte ne oluyor?": today's program section, hero stat w/ live clock + compass watermark, count-up numerals, week bars grow-in, segmented status bar. Structured skeleton. |
| Gün Programı | ✅ | Triage list (Pending-first, inline Onayla/İptal → status endpoint), timeline knots, crew avatar chips, animated capacity meters, assignment Drawer w/ conflict surfacing. Cancel confirm explains refund semantics. |
| Üyeler | ✅ | FilterBar (search + level chips), avatar column, tabbed Drawer (Profil/Paketler/Hareketler), MeterBar balances, TimelineList activity. Create moved to Drawer. |
| Paketler | 🟡 | Catalog cards show derived per-session price ✓. Gap: catalog lacks search once many packages; deactivation confirm exists but "Pasif" cards lack re-activate affordance (backend supports via PUT isActive=true) — add "Yeniden aktifleştir". |
| Kaynaklar | 🟡 | Status segmented filter added ✓. Gap: boat cards don't indicate which sessions today use them (data available via GET /sessions?date=today) — cross-link candidate; defer unless cheap. |
| Ayarlar | ✅ | StickyActionBar dirty-save, closed-dates upcoming/past timeline. Gap: timezone field is free-text; backend accepts any string — add hint listing common TR zones (Europe/Istanbul) as datalist (presentation only). |
| Akış moderasyonu | ✅ | Lazy media + lightbox Dialog, delete confirm, comments/participants expanders. |
| Kullanıcılar | 🟡 | Role select inline ✓ guard errors toasted ✓. Gap: no visual differentiation between own account row (cannot_demote_self risk) — mark caller row "(sen)" using session email. |

## Member Portal

| Screen | Verdict | Findings |
|---|---|---|
| Auth (login/register) | ✅ | Branded band + waves, guided club entry when context missing (FIX-03), expired-session variant. Register shows optional consents; mandatory deferred to first booking (matches backend enforcement point). |
| Home / Booking | ✅ | NextSessionCard answers "sonraki küreğim", options-driven class chips, availability grid w/ seats, Booking-consent gate, package toggle when credit exists. Gap: slot grid buttons could group morning/evening; minor. |
| Randevularım | ✅ | Upcoming/past split, cancel confirm w/ refund copy, masked crewmates. |
| Paketlerim | ✅ | Balance cards w/ exhausted state. |
| Profilim | ✅ | Edit form, OTP channels, consents manager (required locked), cards w/ photo upload validation, hard-delete danger zone w/ password Dialog. |
| Arkadaşlar & Sohbet | 🟡 | Connection manager isolated ✓ unread badges ✓ optimistic-free send ✓. Gaps: (1) connection status not surfaced ("Bağlantı yeniden kuruluyor…"); (2) failed send leaves draft silently — keep text and show inline retry; (3) chat thread lacks day separators. |
| Kulüp Akışı | 🟡 | Composer w/ media+event, like/join toggles server-truth patch ✓. Gaps: media lightbox missing on portal (panel has one); follow button lacks count display (ToggleResult.count = follower count, could show). |

## Public Site

| Screen | Verdict | Findings |
|---|---|---|
| Club landing + booking wizard | ✅ after FIX-01 | Hero w/ pennants + CTA anchor; 2-step wizard; guest 4x restriction explained upfront; phone re-check at step 2 for member-level slots; consent checkboxes inline; conflict errors verbatim from backend. Gap: date fixed to today with no picker — backend supports arbitrary `date` within window; add a simple date input (maxAdvanceDays-bounded) to widen usefulness. |

## Design System

| Area | Verdict | Notes |
|---|---|---|
| Tokens | ✅ | Dual-palette `[data-theme]`, motion tokens, focus ring, scrollbars. |
| Buttons | ✅ | 4 variants × 2 sizes, pending spinner, press physics. Success/error button states not modeled — acceptable (toasts/inline cover feedback). |
| Forms | ✅ | FormField label/hint/error wiring, aria-invalid, consistent controls. |
| Overlays | ✅ | Blur backdrop, focus trap, Esc, return focus. Mobile bottom-sheet behavior: Drawer already full-width <md; Dialog max-w caps fine. |
| Feedback | ✅ | Toaster roles, AsyncBoundary permission/not-found branches, structured skeletons in dashboard; generic TableSkeleton elsewhere — acceptable. |
| Nautical kit | ✅ | WaveDivider/PennantStrip/CompassRose/AnchorArt — used with restraint. |
| Theme infra | ✅ | Pre-paint script ×3, system-preference default, persisted override, `tone="onDark"`. |

## Priority fixes from this audit

1. Portal chat: connection-status banner + failed-send retention (ISSUE-05 below).
2. Portal feed media lightbox (reuse panel pattern).
3. Packages catalog: re-activate passive packages.
4. Users page: mark caller row.
5. Public site: date input on wizard step 1.
6. Settings timezone `<datalist>` hint.

Each fix uses existing verified endpoints only (PUT /packages/{id} isActive=true; GET /feed/{id}/media; no new backend capability required).

## ISSUE-05 (new, discovered during audit)

**Chat send failure loses user input:** `sendMessage` mutation clears draft only onSuccess; onError keeps it — actually correct — BUT there is no visible error surface inside the thread (toast only, may be missed). Add inline failed-message indicator with "Tekrar dene".
