---
name: api-designer
description: >
  Design and review backend API contracts for this repo's Minimal API surfaces — endpoints,
  request/response shapes, validation, errors, pagination, and versioning. Use when adding,
  changing, or reviewing an HTTP endpoint.
---

# Mission

Produce predictable, secure, evolvable API contracts consistent with this repo's existing
surfaces — not a fresh convention invented per endpoint.

# When to Use

- Adding a new endpoint to any of the four surfaces below.
- Changing a request/response shape or error behavior on an existing endpoint.
- Reviewing an endpoint for contract consistency.

# Do Not Use

- Pure architecture/module-boundary decisions — that's `backend-architect`.
- Schema design itself (as opposed to the DTO shape exposed over HTTP) — that's `database-designer`.

# Inputs to Inspect

- `references/rest-conventions.md` — resource naming, verbs, route parameter typing, pagination,
  versioning, soft-deactivation convention.
- `references/error-format.md` — the two error shapes and the reusable error-code dictionary.
- `docs/API_ENDPOINTS.md` — the authoritative, up-to-date contract list; keep it in sync with any
  endpoint you add/change.

# Surfaces

- `/api/v1/auth/*` — identity flows (anonymous + rate limited via `RateLimitingSetup.AuthPolicy`).
- `/api/v1/public/{subdomain}/*` — a company's public site, anonymous; first step is always
  `TenantResolver.ResolveBySubdomainAsync`, 404 `company_not_found` if it fails.
- `/api/v1/company/*` — company panel, `Roles = "CompanyAdmin"` (or `Employee` where applicable),
  tenant resolved from the JWT's `CompanyId`.
- `/api/v1/platform/*` — platform admin, `Roles = "PlatformAdmin"`.

# Hard Rules

## MUST

- MUST validate all external input (body/query/path/headers) — see `02-api-data-contracts.md`.
- MUST use `MapGroup` + `.WithName("PascalCase")` per endpoint (Minimal API convention already
  used throughout).
- MUST define the request body as a `sealed record XxxRequest(...)` at the top of the endpoint
  file.
- MUST return success via `ApiResponse<T>.Ok(data, message?)`; creation via
  `Results.Created(uri, ...)`.
- MUST parse date (`yyyy-MM-dd`) / time (`HH:mm`) strings at the endpoint, returning 400 with
  `invalid_date`/`invalid_time` on failure.
- MUST let `DomainException` propagate from the handler and be converted by
  `GlobalExceptionHandler` — MUST NOT wrap handler calls in a try/catch to reimplement this.
- MUST update `docs/API_ENDPOINTS.md` and, if the endpoint is auth-related or newly
  protected/public, the Functional/Security test endpoint inventories (see `test-engineer`).

## MUST NOT

- MUST NOT expose a Domain entity directly as a response — map to an Application-layer DTO.
- MUST NOT leak internal exception details, DB details, or stack traces in a response.
- MUST NOT invent a new error-code style — reuse the existing dictionary in
  `references/error-format.md` before adding a new snake_case English code with a Turkish
  user-facing message.

## SHOULD

- SHOULD prefer bounded collection responses; add pagination (`?page=&pageSize=`) once a list can
  realistically grow large.
- SHOULD prefer soft-deactivation (`IsActive = false`) over hard delete, matching the existing
  Boat/Instructor/Package pattern.
- SHOULD define idempotency behavior explicitly for anything payment/webhook-shaped (see
  `backend-reliability`).

## MAY

- MAY add `Asp.Versioning` `ApiVersionSet` handling for a breaking change on a surface that
  already uses it (see the Auth endpoints example) — today's `v1` is otherwise fixed in the URL.

# Decision Framework

- Which of the four surfaces does this belong to, and what does that imply for auth/tenant
  resolution?
- Is this a new resource, or an action on an existing one (→ sub-action POST like
  `/appointments/{id}/status`)?
- Does the response shape already exist as a DTO elsewhere, or does it need a new one?
- Could this list grow unbounded — does it need pagination now or is a documented "small by
  construction" assumption enough?

# Workflow

1. Identify the surface and its auth/tenant-resolution requirement.
2. Check `references/rest-conventions.md` for the route/verb shape before inventing one.
3. Define the request record and DTO shape.
4. Let business-rule failures surface as `DomainException`; only handle the endpoint-local early
   returns explicitly (`ApiResponse.Fail`).
5. Update `docs/API_ENDPOINTS.md` and the relevant test inventories.

# Anti-Patterns

- A Domain entity serialized straight into the HTTP response.
- A bespoke error JSON shape that doesn't match either of the two sanctioned formats.
- An unbounded list endpoint added for data that will grow over the product's lifetime.
- A `try/catch` around a handler call that just re-implements what `GlobalExceptionHandler`
  already does.

# Quality Checklist

- [ ] Route/verb matches `references/rest-conventions.md`.
- [ ] Request/response are DTOs, not Domain entities.
- [ ] Errors use one of the two sanctioned shapes with a reused or newly-dictionaried code.
- [ ] `docs/API_ENDPOINTS.md` updated.
- [ ] Test inventories updated if the endpoint is new/removed/reprotected.

# Handoff

- Authorization correctness → `auth-security`.
- Persistence shape backing the DTO → `database-designer`.
- Resilience for anything calling out to a third party → `backend-reliability`.
- Test coverage → `test-engineer`.
