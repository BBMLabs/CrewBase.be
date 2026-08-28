---
paths:
  - "src/**/*.cs"
---

# Backend Engineering Principles

## Simplicity

MUST prefer the simplest design that satisfies current requirements.

MUST NOT create abstractions solely because they may be useful later.

Avoid speculative:
- microservices
- event buses
- repositories beyond what a module already needs
- managers
- factories
- providers
- generic wrappers
- internal frameworks

This repo is a deliberate modular monolith (see `backend-architect`'s
`references/architecture-rules.md`) — a new service boundary is not the default answer to a new
requirement.

---

## Cohesion

Modules SHOULD have a clear responsibility. Related business behavior SHOULD remain close
together (see `backend-architect`'s `references/module-patterns.md` for this repo's
Domain/Application/Infrastructure split per module).

Avoid splitting code solely to achieve small file sizes.

---

## Dependencies

Dependencies SHOULD flow toward stable business logic.

Domain behavior SHOULD NOT depend unnecessarily on:
- HTTP
- framework APIs
- database ORM details
- queue implementations

This is enforced mechanically here: `tests/RowingClub.ArchitectureTests` fails the build if
Domain references EF Core/MediatR/FluentValidation, if Application references Infrastructure, or
if one module's Infrastructure references another module's.

---

## Side Effects

Side effects SHOULD be explicit.

Avoid hidden:
- database writes
- network requests
- event publishing
- mutable global state

In this codebase: a write request is `ICommand<T>`; a tenant-writing handler MUST call
`ISchedulingUnitOfWork.SaveChangesAsync` itself at the end of `Handle` — this is not automatic the
way `TransactionBehavior` automatically commits catalog-DB writes for `ICommand<T>`. Forgetting
this call silently discards the change.

---

## Error Handling

Errors SHOULD retain useful internal context.

External responses MUST NOT expose sensitive implementation details.

Expected failures SHOULD be represented explicitly: `DomainException(code, message)` for business
rule violations (`code` snake_case English, `message` Turkish and user-facing),
`NotFoundException` for missing resources. `GlobalExceptionHandler` converts these to
ProblemDetails — do not add a try/catch in an endpoint to do this manually.

Unexpected failures SHOULD be observable — see `04-reliability-observability.md`.

---

## Configuration

Configuration MUST be environment driven where appropriate. All settings come from env
(`DotEnvFileLoader` → `.env.developer`/`.env.production`, selected by `APP_ENV`) — see
`production-readiness` skill for the full inventory.

Secrets MUST never use insecure source-controlled defaults. A missing critical secret (JWT keys,
field encryption keys) SHOULD fail clearly at startup, not silently degrade.
