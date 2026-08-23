# CrewBase — Implementation Plan (phased, status-tracked)

> Gates per phase: `typecheck ✓ lint ✓ test ✓ build ✓` (+ real-API verification when backend
> is running). Endpoint statuses: VERIFIED / IMPLEMENTED / BACKEND_INCOMPLETE / NOT_VERIFIED /
> BLOCKED — maintained in API-INVENTORY.md; UI mapping status in ENDPOINT-UI-MAP.md.

## Phase 0 — Documentation & inventory ✅ (this phase)

- [x] API-INVENTORY.md (all ~90 endpoints verified against source)
- [x] DOMAIN-MAP.md · AUTHORIZATION-MATRIX.md · ENDPOINT-UI-MAP.md · FEATURE-MATRIX.md
- [x] FRONTEND-ARCHITECTURE.md · DESIGN-SYSTEM.md · IMPLEMENTATION-PLAN.md

## Phase 1 — Foundation

1. Root workspace: package.json, tsconfig.base.json, eslint/prettier, .gitignore additions.
2. `packages/config` shared ts/vite/eslint presets.
3. `packages/api-types`: envelope + ProblemDetails + all DTO types (cited to C# sources) +
   enums-as-string unions.
4. `packages/api-client`: request core (timeout/abort/correlation), error normalization →
   AppError + Turkish catalog, auth kernel + companyUserAuth/memberAuth/publicAuth strategies,
   subdomain helper, services: authApi/companyApi/memberApi/publicApi (full surface).
5. `packages/design-system`: tokens layer, labels/tr.ts, core primitives used by Phase 2.
6. Gate: root typecheck/lint/test/build green with smoke tests for client+errors.

## Phase 2 — Company Panel (primary product)

Order: app bootstrap (providers/router/guards/env) → Auth flows → Shell (nav/layout) →
Genel Bakış → Gün Programı (appointments + sessions + assignment) → Üyeler (directory,
drawer: level/packages/logs) → Paketler & Bakiyeler → Kaynaklar → Ayarlar (+closed dates)
→ Kulüp Akışı moderasyon → Kullanıcılar.
Every feature wired to real endpoints at implementation time (no mock-first), MSW only in ITs.
Real-API verification pass against `http://localhost:5283` when backend is up.

## Phase 3 — Member Portal

Bootstrap w/ subdomain-aware public layout → login/register → protected shell →
Ana sayfa booking workflow (options→availability→consents→confirm) → Randevularım →
Paketlerim → Profil (OTP, consents, cards, delete-account danger zone) → Arkadaşlar &
Sohbet (connection manager + thread UI) → Kulüp Akışı (composer, feed, comments,
participants, follow).

## Phase 4 — Public Site

Landing (info/options) → availability explorer → guest booking wizard (identity fields,
class restriction UX for guests, mandatory consents every booking, conflict handling,
confirmation state). Mobile-first conversion flow.

## Phase 5 — Verification & closure

1. Test deepening: unit (mappers/error catalog/auth kernels/query keys/date utils),
   integration per feature (MSW contract-faithful incl. ProblemDetails + validation maps),
   E2E Playwright scaffold (auth/, company-panel/, member-portal/, public-site/) with
   real-backend profile via env (`E2E_API_URL`, health-probe gating) + documented docker run.
2. ENDPOINT-COVERAGE.md audit (counts by disposition; no silent gaps).
3. Final report (per template §52) incl. backend observations (e.g., committed secrets in
   deploy/docker/docker-compose.yml, Employee-role gap, no-pagination scaling notes).

## Explicitly out of scope

- apps/platform-admin (architecture-ready, not built).
- /admin/* placeholder screens, 2FA (backend unmapped), Employee workspace (BACKEND_INCOMPLETE).
- Any pagination invention; any invented endpoints/fields/rules.

## Risks & boundaries

- Member token expiry UX depends on accurate `expiresAtUtc` handling (no refresh exists).
- Feed/media base64 payloads may be large on slow links — size limits enforced client-side
  mirroring backend (5MB image / 25MB video).
- `/company/appointments` without date returns unbounded history — panel always passes date.
- If a handler's error code differs from docs during wiring, code wins and the inventory row
  is corrected the same commit.
