---
name: feature-planner
description: >
  Break a feature request into a concrete, ordered implementation plan before writing code. Not
  backend-specific — usable for any substantial engineering task in either repo — but this file's
  concrete layer sequence is CrewBase.be's own. Use before starting any substantial feature.
---

# Mission

Produce the smallest safe implementation plan before substantial work starts, grounded in this
repo's actual layering — not a generic project-planning template.

# When to Use

- Any feature substantial enough to touch more than one layer or file.
- Before invoking `backend-architect`/`api-designer`/`database-designer` on a new feature, to
  scope what each of them actually needs to do.

# Do Not Use

- A one-file, one-line fix — just make the change.
- Mid-incident triage — that's `debugger`.

# Inputs to Inspect

- The existing architecture for the module the feature likely belongs to (see
  `backend-architect`'s `references/module-patterns.md`).
- Whether a similar feature already exists to copy the shape of, end to end.

# Hard Rules

## MUST

- MUST inspect existing architecture before proposing a new abstraction — see
  `01-backend-principles.md` §Simplicity.
- MUST identify, up front, which DB (catalog or tenant) the feature's data belongs to.
- MUST identify which existing tests will break (handler ctor signature changes, endpoint
  inventories) as part of the plan, not as a surprise during implementation.

## SHOULD

- SHOULD scope a large feature to its thinnest possible end-to-end vertical slice first, then
  widen — not build every layer in full before anything is wired together.

## MAY

- MAY skip a formal written plan for a feature small enough that the steps below fit in one
  paragraph — the discipline matters more than the ceremony.

# Decision Framework

- Which module owns this (Identity / Scheduling / a new module)? Catalog DB or tenant DB?
- What's the vertical slice that proves the feature end to end fastest?
- What existing tests/inventories will this touch?

# Workflow

1. **Domain analysis** — which module, which DB, what are the entity/value-object rules? Business
   rule violations become `DomainException(code, Turkish message)`.
2. **Application** — Command/Query + Handler (+ Validator). Writes are `ICommand<T>`; a
   tenant-writing handler ends with `ISchedulingUnitOfWork.SaveChangesAsync`.
3. **Infrastructure** — repository implementation, EF mapping, DI registration. If the schema
   changed, note which context needs a migration (`RowingClubDbContext` catalog /
   `TenantDbContext` tenant) — see `database-designer`/`migration-manager`.
4. **API** — endpoint (public → tenant resolved from subdomain; panel → from `CompanyId`; role
   check via `RequireAuthorization`), request record, `ApiResponse<T>` response — see
   `api-designer`.
5. **Fix what breaks** — unit tests for any handler whose constructor changed; Functional/Security
   endpoint inventories if the endpoint surface changed.
6. **Verify** — `dotnet build` → `dotnet test` → run the API and drive the real end-to-end flow
   with curl (e.g. register → site → booking → panel).

# Anti-Patterns

- A plan that designs every layer in full detail before anything has been proven to work
  end-to-end.
- A plan with no mention of which tests will need updating.
- Proposing a new module/abstraction before checking whether an existing one already fits.

# Quality Checklist

- [ ] Module and DB (catalog/tenant) ownership are explicit.
- [ ] Plan is a short file list + what changes at each step, not a long essay.
- [ ] Test/inventory impact is identified up front.
- [ ] Large features are scoped to a thin vertical slice first.

# Handoff

- Architecture/module-boundary decisions → `backend-architect`.
- API contract shape → `api-designer`.
- Schema design → `database-designer`.
- Auth/permission dimension → `auth-security`.
