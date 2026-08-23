# CrewBase — Endpoint → UI Map

> Traceability chain per user-facing endpoint:
> `Endpoint → Audience → Feature/Workflow → Screen → Query/Mutation → Service fn → Guard → Test`
> Status: IMPLEMENTED / PLANNED / NO_UI / AUTH_INFRA / NOT_USER_FACING.
> Live verification of all rows is BLOCKED by a backend build break (see API-INVENTORY.md
> "Backend build status"). Final counts live in ENDPOINT-COVERAGE.md.

## Conventions

- Query keys: `panelKeys.*` (company panel), `memberKeys.*` (portal), `publicKeys.*` (site).
- Services: `packages/api-client/src/services/{auth-service,company-service,member-service,public-service}.ts`.
- Tests: unit (logic) / IT (Vitest+MSW integration) / E2E (Playwright, real backend).

## 1. Auth — Panel identity flows

| Endpoint | Chain | Status |
|---|---|---|
| POST `/auth/login` | GirişPage → session.login() → zustand store → public route | AUTH_INFRA · IMPLEMENTED |
| POST `/auth/refresh` | single-flight kernel on 401/expiry timer → replay once; reuse ⇒ hard logout | AUTH_INFRA · IMPLEMENTED |
| POST `/auth/logout` | sidebar footer + confirm dialog → session.logout() | AUTH_INFRA · IMPLEMENTED |
| POST `/auth/logout-all` | sidebar footer ("Tüm cihazlardan çıkış") + confirm | AUTH_INFRA · IMPLEMENTED |
| POST `/auth/companies/register` | KulüpKaydıPage (pw rules mirrored) → success shows subdomain/siteUrl | IMPLEMENTED |
| POST `/auth/forgot-password` / `/reset-password` | dedicated pages; generic success copy; query-param token support | IMPLEMENTED |
| POST `/auth/verify-email` / `/send-verification-email` | OTP page w/ 6-digit input + resend | IMPLEMENTED |

## 2. Platform — future app

GET/POST `/platform/*` (5 endpoints): **NO_UI this pass** — reserved for apps/platform-admin.

## 3. Admin placeholders — NOT_USER_FACING (no UI this pass)

## 4. Public site (Phase 4)

| Endpoint | Chain | Status |
|---|---|---|
| GET `/public/{sub}/info` | club landing header/contact → useInfo(sub) | IMPLEMENTED |
| GET `/public/{sub}/options` | booking wizard config (hours, classes+capacity, reminders) + info tiles | IMPLEMENTED |
| GET `/public/{sub}/consents` | consent step rendering (Booking scope, every booking) | IMPLEMENTED |
| GET `/public/{sub}/availability` | slot grid per date/class; step-2 re-check with phone for member-level matching | IMPLEMENTED |
| POST `/public/{sub}/appointments` | 2-step guest wizard; conflicts surfaced verbatim (`slot_full`, `guest_class_restricted`, `consents_required`, `already_booked`); confirmation view w/ appointment id | IMPLEMENTED |
| GET `/site/{sub}`, `/` | backend-rendered HTML | NO_UI |

Member auth: POST `/members/register`, `/members/login` → Portal auth screens.

## 5. Member portal (Phase 3)

| Endpoint | Chain | Status |
|---|---|---|
| GET/PUT/DELETE `/me`, OTP pair | Profilim: edit form, verify panels, danger-zone delete (password confirm) | IMPLEMENTED |
| GET `/appointments` · cancel | Randevularım: upcoming/past split, masked crewmates, refund-aware cancel confirm | IMPLEMENTED |
| POST `/appointments` | Ana sayfa workflow: options → date/class → availability grid → Booking consents if missing → usePackage toggle → book → invalidate availability+appointments+packages+consents | IMPLEMENTED |
| GET `/packages` | Paketlerim balance cards | IMPLEMENTED |
| GET/POST `/consents` | Beyanlarım section; required disabled-checked; optional revocable | IMPLEMENTED |
| GET/PUT `/cards` | Kartlarım: Multisport digits-only number, Meditopia company name, photo ≤5MB base64 | IMPLEMENTED |
| GET `/code`, friends cluster | Arkadaşlar: my-code card + copy, incoming/outgoing/friends sections, unread badges | IMPLEMENTED |
| messages pair + SignalR hub | Sohbet thread; chat-connection-manager (ref-counted lifecycle, reconnect resync, dedup by id); optimistic-free send via REST then cache update | IMPLEMENTED |
| feed cluster | Akış: composer (media/event), like/join toggles w/ server-truth patch, comments add, participants expander, follow toggle, own-post delete confirm | IMPLEMENTED |

## 6. Company panel (Phase 2)

| Endpoint | Chain | Status |
|---|---|---|
| GET `/company/site` | shell header club name+link; settings site card | IMPLEMENTED |
| GET `/company/stats` | dashboard stat cards, next7Days bars (real data), status distribution | IMPLEMENTED |
| GET `/company/appointments?date=` | Gün Programı left list (date-scoped only) | IMPLEMENTED |
| POST `/company/appointments/{id}/status` | row select + cancel-confirm w/ refund copy; invalidates appointments+sessions+stats | IMPLEMENTED |
| GET `/company/sessions?date=` | Gün Programı right cards w/ capacity meter + roster | IMPLEMENTED |
| POST `/company/sessions/{id}/assign` | assign dialog; `boat_taken`/`instructor_busy` inline; invalidates sessions+appointments | IMPLEMENTED |
| GET/POST `/customers` | directory table + client search + create drawer (zod mirrors server rules) | IMPLEMENTED |
| POST `/customers/{id}/level` | drawer level select (0–10 backend labels) | IMPLEMENTED |
| POST `/customers/{id}/packages` · GET | drawer package assign + balances list | IMPLEMENTED |
| GET `/package-balances` | Bakiyeler tab table w/ progress bars | IMPLEMENTED |
| GET `/customers/{id}/logs` · `/logs` | member drawer log section + Son Hareketler tab (event label map) | IMPLEMENTED (live check BLOCKED) |
| CRUD `/boats` | Kaynaklar tab Tekneler; class select w/ capacity display | IMPLEMENTED |
| CRUD `/instructors` | Kaynaklar tab Eğitmenler; active toggle via pasife al | IMPLEMENTED |
| CRUD `/packages` | Paketler Katalog tab; price/session forms; deactivate confirm | IMPLEMENTED |
| GET/PUT `/settings` | settings form (time inputs HH:mm, openDays toggles, notice window, reminder options, timezone) | IMPLEMENTED |
| CRUD `/closed-dates` | settings closed-dates manager (add/remove) | IMPLEMENTED |
| GET/POST `/feed` · delete/comments/participants | Kulüp Akışı moderation: club-author composer (media+event), delete-any confirm, comments/participants expanders | IMPLEMENTED |
| GET `/users` · POST · role change | Kullanıcılar page: create dialog, inline role select; guard errors toasted | IMPLEMENTED |

### Redesign notes — deep UI/UX pass 2 (2026-08)

Information-architecture changes without endpoint changes:

- **Gün Programı**: appointments render as a *triage list* (Pending-first, inline Onayla/İptal
  buttons → same `POST /appointments/{id}/status`); session cards gained crew avatar chips,
  animated `MeterBar` capacity, and assignment moved into a Drawer (same `assign` + boats +
  instructors queries).
- **Üyeler**: `FilterBar` (search + client-side level filter), avatar column, quick "Detay"
  action; member detail is a **tabbed Drawer** (Profil / Paketler / Hareketler) using the same
  level, packages, logs endpoints; create-member moved to a Drawer.
- **Paketler**: catalog cards show derived per-session price (presentation arithmetic only);
  Balances tab regrouped by member with per-package meters + client-side search.
- **Kaynaklar**: status segmented filter (client-side) for both tabs.
- **Ayarlar**: dirty-detection **StickyActionBar** (Kaydet/Vazgeç); closed dates split into
  upcoming/past timeline (`TimelineList`).
- **Akış moderasyonu**: lazy media load via `GET /feed/{id}/media` + click-to-zoom Dialog.
- **Theme**: all three apps support light/dark via `[data-theme]` token scope, persisted in
  localStorage, system-preference default, `ThemeToggle` placed per app shell.

## 7. System

| Route | Status |
|---|---|
| `/health` | used by E2E readiness probe; NO_UI |
| `/metrics`, `/openapi/v1.json`, `/scalar/v1` | NO_UI |
| `/hubs/chat` | see §5 |
