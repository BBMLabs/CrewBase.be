# CrewBase — Frontend Authorization Matrix

> Backend is the security authority. Frontend authorization is UX-only (route guards, hiding
> disabled actions). Verified against `AUTHORIZATION_MATRIX.md` + endpoint source + handlers.

## Roles & identity spaces

| Role | Identity space | JWT claims | Token model |
|---|---|---|---|
| `PlatformAdmin` | catalog `identity_users` | `sub`=userId, `role`, no company_id | access 15 min + refresh rotation |
| `CompanyAdmin` | catalog `identity_users` | `sub`=userId, `role`, `company_id` | access 15 min + refresh rotation, reuse detection |
| `Employee` | catalog `identity_users` | same as CompanyAdmin | same — but **no real endpoints yet** (placeholder only) |
| `Member` | tenant DB `Customer` | `sub`=customerId, `role=Member`, `company_id` | **access token ONLY — no refresh; re-login on expiry** |

One role per user (`role` claim singular). Frontend apps never mix spaces: Panel uses
`/auth/*`; Portal uses `/public/{subdomain}/members/*`.

## Enforcement layers the UI can rely on

1. Route-level `[Authorize(Roles=...)]` → 401/403.
2. Tenant: server resolves from subdomain route or `company_id` claim → env 404
   `company_not_found` (UI must render "firma bulunamadı" state, not a crash).
3. Resource ownership inside handlers (e.g., member cancel own appointment only;
   friend-respond addressee only; post delete owner/moderator) → 403/409 domain errors.
4. Double-guard on panel user management: handler reloads caller from DB each call
   (`caller_deactivated`, `forbidden`, `cannot_demote_self`).

## Endpoint groups × frontend guard mapping

| Group | Role gate | Tenant source | Frontend guard |
|---|---|---|---|
| `/api/v1/auth` (anon subset) | none | none | public routes |
| `/api/v1/auth/logout-all` | any authenticated | none | requires session |
| `/api/v1/platform/*` | PlatformAdmin | platform DB | **not built this pass** |
| `/api/v1/admin/*` | mixed (placeholders) | route companyId == claim | not built (NOT_USER_FACING) |
| `/api/v1/public/{sub}/*` incl. members register/login | anonymous | subdomain route | public routes (subdomain-aware) |
| `/api/v1/member/*` | Member | company_id claim | portal protected layout |
| `/hubs/chat` | Member | connection key `{company_id}:{sub}` | portal-only connection manager |
| `/api/v1/company/*` | CompanyAdmin | company_id claim | panel protected layout |

## Frontend permission model (UX)

Central helper in `packages/api-client/auth`:

```ts
type Role = "PlatformAdmin" | "CompanyAdmin" | "Employee" | "Member";
can(role, "panel.access")        // CompanyAdmin
can(role, "panel.users.manage")  // CompanyAdmin (backend double-checks anyway)
can(role, "member.self")         // Member — everything under /member is self-scoped
```

No fine-grained permission strings exist in the backend (roles only). The frontend therefore
models **role gates**, not invented permissions. Employee-role UX: backend provides nothing
real → if an Employee logs into the panel they will see navigation but every data call fails
403; UI shows explicit "bu rol için erişilebilir bir alan tanımlı değil" state instead of
fake content. Documented as BACKEND_INCOMPLETE.

## Error → UX semantics (authorization-related)

| Backend signal | HTTP | UI behaviour |
|---|---|---|
| missing/invalid/expired token | 401 `unauthorized` / ProblemDetails | Panel: silent refresh once → login redirect. Portal: session-expired notice → login |
| role/policy rejection | 403 `forbidden` | inline "yetkiniz yok" alert; action stays hidden/disabled by guard |
| cross-company target | 403 `forbidden` (domain) | toast with server message |
| tenant resolution failure | 404 envelope `company_not_found` | dedicated empty-state page (not generic 404) |
