# CrewBase — Endpoint Coverage Audit

> Date: 2026-08-22 · Source: verified endpoint files (`src/RowingClub.Api/Endpoints/*.cs`).
> Statuses: VERIFIED / IMPLEMENTED / BACKEND_INCOMPLETE / NOT_VERIFIED / BLOCKED.

## Headline numbers

| Metric | Count |
|---|---|
| HTTP operations discovered (incl. system routes, excl. dev-only tooling) | **92** |
| UI-facing endpoints | **81** |
| Implemented in frontend (wired to real API) | **81 / 81** |
| Infrastructure-only (health probe; no product UI) | 1 (+1 hub = realtime infra) |
| NO_UI by design (platform ×5, admin placeholders ×4, HTML site ×2, dev tools openapi/scalar) | 13 |
| Contract-verified | 92 / 92 (100%) |
| Live end-to-end verification | **BLOCKED** — backend does not compile (see below) |

## Per-group breakdown

| Group | Endpoints | Implemented UI | Notes |
|---|---|---|---|
| `/auth/*` | 9 | 9/9 | login, refresh kernel, logout(+all), forgot/reset, email OTP, company signup |
| `/company/*` | 37 | 37/37 | dashboard→users; logs endpoints carry runtime caveat below |
| `/member/*` | 30 | 30/30 | profile/OTP/appointments/packages/consents/cards/friends/messages/feed/follow |
| member auth (`/public/{sub}/members/*`) | 2 | 2/2 | subdomain-scoped; short-lived token UX |
| public site API (`/public/{sub}/*`) | 5 | 5/5 | info/options/consents/availability/guest booking |
| SignalR `/hubs/chat` | 1 | 1/1 | connection manager + thread receive path |
| `/platform/*` | 5 | 0 | future apps/platform-admin (documented, architecture-ready) |
| `/admin/*` placeholders | 4 | 0 | NOT_USER_FACING (placeholder text endpoints) |
| System: `/health` `/metrics` `/openapi/v1.json` `/scalar/v1` `/site/{sub}` `/` | 6 | health used by E2E readiness only | rest NO_UI |
| 2FA routes | 0 live | — | deliberately unmapped in backend → 404; no UI built |

## Status roll-up

- **VERIFIED:** 92/92 contracts confirmed against source (DTOs read from handlers/records).
- **IMPLEMENTED:** all 81 UI-facing endpoints have feature → hook → service wiring shipped
  (apps/company-panel 100%, apps/member-portal 100%, apps/public-site 100% of its group).
- **BACKEND_INCOMPLETE:** `RowingClub.Scheduling.Domain.Logs` namespace is absent from the repo
  while ~10 Application files import it ⇒ the solution does not compile. Affected runtime paths:
  company activity-log endpoints (7.13/7.14), and any flow touching MemberLog writes.
  The HTTP contract itself was implemented from intact handler code.
- **BLOCKED (live verification):** every endpoint's real-request verification is blocked until
  the backend owner restores the missing domain layer. Playwright suite is in place and will
  fail fast with setup instructions when `/health` is unreachable (by design, no mock fallback).
- **NOT_VERIFIED:** none at contract level.

## Known gaps & risks carried forward

1. Backend build break (above) — single blocker for integration verification.
2. Committed secrets in `deploy/docker/docker-compose.yml` (live DB/Redis credentials + PEM keys)
   — flagged for the backend owner; not a frontend concern but security-relevant.
3. No pagination anywhere: `/company/customers`, `/package-balances`, feed(50) etc. are unbounded;
   panel always date-scopes appointments/sessions. Proposed backend improvement list lives in
   API-INVENTORY.md.
4. Employee role has no real endpoints (placeholder only) — panel shows an explicit empty state,
   no fake content.
5. Member tokens cannot refresh (backend has no such endpoint); portal implements expiry→re-login.

## How to run the E2E suite once the backend is fixed

```bash
# terminal 1 — backend (requires filled .env.developer)
dotnet run --project src/RowingClub.Api

# terminal 2 — frontends
cd frontend && npm run dev:panel        # :5173
npm run dev:portal                      # :5174  (?club=<sub> or VITE_CLUB_SUBDOMAIN)
npm run dev:site                        # :5175

# terminal 3 — tests (fail fast if /health is down)
cd frontend/e2e
E2E_COMPANY_EMAIL=... E2E_COMPANY_PASSWORD=... E2E_MEMBER_SUBDOMAIN=<sub> npx playwright test
```
