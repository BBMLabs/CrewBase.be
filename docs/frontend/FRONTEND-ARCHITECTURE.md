# CrewBase — Frontend Architecture

## 1. Monorepo layout

npm workspaces (Node 24, npm 11). No framework monorepo tooling beyond workspaces +
TypeScript project references where useful.

```text
frontend/
├── package.json                 # private root; workspaces: apps/*, packages/*
├── tsconfig.base.json           # strict: strict, noUncheckedIndexedAccess, noUnusedLocals/Parameters,
│                                # exactOptionalPropertyTypes, verbatimModuleSyntax
├── eslint.config.js  .prettierrc.json
├── apps/
│   ├── company-panel/           # Vite + React + TS. Port 5173.
│   ├── member-portal/           # Vite + React + TS. Port 5174. Subdomain-aware.
│   ├── public-site/             # Vite + React + TS. Port 5175. Anonymous booking.
│   └── platform-admin/          # NOT CREATED (documented future app; consumes packages/* as-is)
├── packages/
│   ├── api-types/               # DTO types hand-mapped from C# records (see §3)
│   ├── api-client/              # HTTP core, error contract, auth strategies, tenant helpers,
│   │                            # feature services (auth/company/member/public), query-key utils
│   ├── design-system/           # tokens, primitives, labels/tr.ts
│   └── config/                  # shared eslint/ts/vite presets
├── e2e/                         # Playwright (real backend mode)
└── docs/                        # this folder
```

Dependency rule: `apps → packages` only; packages never import from apps; api-types has zero
runtime deps.

## 2. Runtime stack

| Concern | Choice | Notes |
|---|---|---|
| UI | React 19 + Vite | |
| Router | React Router v7 (library mode) | data routers for loaders where helpful |
| Server state | TanStack Query v5 | retry disabled by default (see §6) |
| Client state | Zustand | auth session metadata + UI prefs only; never server entities |
| Forms | React Hook Form + Zod | schemas mirror backend FluentValidation rules |
| Styling | Tailwind CSS v4 + CSS custom-property token layer in design-system | light-first theme |
| Realtime | @microsoft/signalr | member portal chat only, via connection manager |
| Tests | Vitest + Testing Library + MSW (tests only) | Playwright for E2E |

## 3. Types strategy

OpenAPI generation is **not viable today** (verified): only `/auth/*` carries `.Produces<>`
metadata (~9 of ~90 endpoints), several responses are anonymous objects, and `/openapi/v1.json`
exists only in Development. Therefore:

- `packages/api-types/src/*.ts` — hand-mapped interfaces mirroring C# record shapes with
  exact camelCase JSON names. Each type header cites its C# source file.
- No generated code exists ⇒ no "never edit generated" risk yet. If the backend later emits
  full OpenAPI metadata, introduce `openapi-typescript` into `packages/api-types/src/generated/`
  and delete hand mappings incrementally.
- Mappers (`api/mappers.ts` per feature) convert DTO → view model only when shapes diverge;
  identity reuse is allowed to avoid ceremony.

## 4. API client design

```text
UI → feature hook (TanStack Query) → service fn (packages/api-client/services/*)
   → apiClient.request() → fetch → backend
```

- One generic `request<T>(opts)`; methods `get/post/put/delete`. AbortController per call;
  caller cancellation supported through Query signal propagation.
- Timeout default 15s (configurable); network failures → `AppError{kind:"network"}`.
- Response handling: 2xx → parse envelope `{success,data,message,code}`; non-2xx JSON that is a
  ProblemDetails (`application/problem+json` or has `traceId`+`code`) → ProblemDetails branch;
  envelope-failure shape (`success:false`) → envelope branch; else generic.
- Correlation id header `X-Correlation-ID` (uuid per app session) attached to every request.
- Base URLs per app via env: `VITE_API_BASE_URL` (default `http://localhost:5283`).
- Retry policy: **0 everywhere** (mutations non-idempotent; auth endpoints rate-limited 10/min).
  Optional GET retry later behind explicit config flag only.
- Auth injection via strategy objects (§5). Services are plain functions taking an
  `ApiClient` instance bound at bootstrap — no singletons inside components.

## 5. Auth strategies (explicit, not one generic abstraction)

Shared kernel: token storage adapter (localStorage/sessionStorage), JWT payload reader,
single-flight refresh queue, 401 interceptor hook points.

| Strategy | App | Behaviour on 401 / expiry |
|---|---|---|
| `companyUserAuth` | company-panel | access+refresh persisted (localStorage; documented tradeoff — backend is header-Bearer, cookies impossible). On 401: single-flight POST /refresh once; success→replay request; failure/reuse-detection→hard logout + login redirect. Proactive refresh timer at `expiresAtUtc - 60s`. Logout calls /logout then clears. |
| `memberAuth` | member-portal | sessionStorage only. NO refresh call ever (endpoint doesn't exist). Expiry known from `expiresAtUtc`: proactive "oturumunuz sona erdi" re-login screen at expiry−30s or first 401. |
| `publicAuth` | public-site | none — anonymous. |
| `platformAdminAuth` | future | same kernel as companyUserAuth; different role gate. |

Session store (Zustand): `{role, email|member summary, companyId?, expiresAtUtc?, status}` —
never raw tokens in React state; tokens live only in storage adapters read by fetch layer.

## 6. Server state conventions

- Query keys include audience scope + parameters:
  `["panel","appointments","list",{date}]`, `["member","feed"]`,
  `["public",subdomain,"availability",{date,class}]`. User change ⇒ `queryClient.clear()`.
- Mutations invalidate precisely (per ENDPOINT-UI-MAP notes), e.g.
  setAppointmentStatus → appointments.list(date), sessions.list(date), panel.stats.
- Optimistic updates ONLY for feed like/join/follow toggles (reversible ToggleResult semantics);
  everything else pessimistic.
- All lists unbounded (no pagination in backend). Date-scoping enforced in UI where the API
  supports it; client-side filtering documented in API-INVENTORY §Scalability notes.

## 7. Error UX contract

`AppError { kind: envelope|problem|network|timeout|cancelled, code?, status?, message?,
fieldErrors?: Record<string,string[]>, traceId? }`.

- Known codes → semantic Turkish copy from catalog (`packages/api-client/errors/catalog.ts`);
  unknown → server `message`/`detail`; always show traceId on persistent errors.
- Validation `errors` map → RHF `setError` per field via resolver adapter.
- Every screen implements states: loading (skeleton), refreshing (subtle indicator),
  empty, error (retry), permission-denied (role guard fallback), not-found, conflict inline,
  submitting (button pending). Enforced by shared `<AsyncBoundary>` + form patterns.

## 8. Realtime (chat) isolation

Only member-portal contains SignalR. `features/chat/realtime/chat-connection-manager.ts`:
lazy start after login; acquire/release ref-counting (StrictMode-safe); `withAutomaticReconnect`
plus manual resync (refetch conversation + friends on reconnect); token supplied once at
negotiate via `?access_token=`; on 401/negotiate failure triggers session-expired flow;
incoming `"message"` events written into TanStack cache; dedup by message id; cleanup on
logout/user change (stop + remove handlers + clear caches). Token never logged.

## 9. Tenant handling

No tenant ids in requests. Subdomain helper mirrors `TenantResolver.FromHost`
(`*.localhost` dev, `*.faturebase.com` prod, excludes `www`/dotted). Portal stores subdomain
at login in session store; public-site derives it from host with `?club=` dev fallback.
Tenant switch = full session change (clear query cache, reset stores, close realtime).

## 10. Build/quality gates

Per workspace and root: `typecheck` (tsc --noEmit, strict), `lint` (eslint flat config),
`test` (vitest run), `build` (vite build). Root scripts aggregate via
`npm run -ws --if-present`. CI-ready ordering: install → typecheck → lint → test → build →
(E2E profile, opt-in, real backend).
