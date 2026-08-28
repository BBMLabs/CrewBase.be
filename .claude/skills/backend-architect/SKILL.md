---
name: backend-architect
description: >
  Design backend module boundaries, service structure, dependency direction, data flow,
  background processing, and system architecture for this repo's .NET 10 modular monolith. Use
  for substantial backend features, architectural refactors, service boundaries, and cross-module
  changes. Not for trivial endpoint edits, isolated SQL tuning, or simple validation changes.
---

# Mission

Design the smallest backend architecture that cleanly supports the current requirements and this
repo's actual operational constraints — a .NET 10 modular monolith with a database-per-tenant
model, not a hypothetical microservice topology.

# When to Use

- Substantial backend features spanning multiple layers/modules.
- New module boundaries, background workers, caching architecture.
- Cross-domain dependencies (one module needing something another module owns).
- Major refactors touching layering or module structure.

# Do Not Use

- Trivial endpoint edits.
- Visual frontend work (that lives in the separate CrewBase.fe repo).
- Isolated SQL/index optimization with no structural implication — see `backend-performance`/`database-designer`.
- Simple validation changes.

# Inputs to Inspect

- `references/architecture-rules.md` — the layering/tenancy/PII rules `RowingClub.ArchitectureTests`
  actually enforces.
- `references/module-patterns.md` — the concrete file/class shape each layer follows, copied from
  an existing module rather than invented.
- The module you're touching's existing `Domain`/`Application`/`Infrastructure` folders for the
  established pattern before adding a new one.

# Hard Rules

## MUST

- MUST decide which module a new business capability belongs to before writing code; if no
  module fits, scaffold a new one by copying the existing Scheduling/Identity layout.
- MUST keep `src/BuildingBlocks/*` free of any dependency on a specific module — it is the shared
  core every module depends on, never the reverse.
- MUST run `tests/RowingClub.ArchitectureTests` after any architectural change and keep it green.
- MUST route cross-module needs through MediatR query/command (Application-level) or a
  BuildingBlocks abstraction first; a direct project reference is the last resort and only ever
  to another module's `Contracts` project.
- MUST put cross-cutting concerns (encryption, idempotency, tenancy) in BuildingBlocks, not
  duplicated per module.
- MUST have every tenant-data code path pass through `ITenantDatabase.Set(...)` before it reaches
  `TenantDbContext` — the context deliberately throws otherwise, to prevent a silent write to the
  wrong (or no) tenant database.

## MUST NOT

- MUST NOT introduce a microservice/new deployable without a concrete operational reason — this
  is a modular monolith by design.
- MUST NOT create an abstraction only for hypothetical future reuse.
- MUST NOT let one module's `Infrastructure` reference another module's `Infrastructure` — share
  through BuildingBlocks or Contracts instead.
- MUST NOT let `Api`/`Bootstrapper` types be referenced from inside a module.

## SHOULD

- SHOULD separate stable business logic from infrastructure details (the existing
  Domain/Application/Infrastructure split already does this — follow it).
- SHOULD keep `RowingClub.Bootstrapper` as the one place that wires up a new module's DI.

## MAY

- MAY add a new BuildingBlocks abstraction when a second module genuinely needs the same
  cross-cutting behavior — not preemptively for the first.

# Decision Framework

Before designing, ask:
- What owns this behavior — which existing module, or does it need a new one?
- Where does the source of truth live — catalog DB (company/user identity) or a tenant DB
  (member/appointment/resource data)?
- What consistency is required — does this need to be inside the same transaction as something
  else?
- What happens when a dependency fails (see `backend-reliability`)?
- Does this genuinely require synchronous processing, or can it be a background job
  (`ReminderWorker`/`SubscriptionSafetyNetWorker` are the reference patterns)?
- What is the simplest viable architecture that satisfies the above?

# Workflow

1. Inspect the target module's existing layers and a sibling feature's file layout.
2. Decide module ownership and DB (catalog vs. tenant).
3. Identify cross-module dependencies and route them through MediatR/BuildingBlocks/Contracts.
4. Design the smallest structure that satisfies the requirement — resist adding a layer/interface
   that has no second consumer yet.
5. Implement, then run `RowingClub.ArchitectureTests` to confirm no layering violation was
   introduced.

# Anti-Patterns

- A new microservice/queue/cache introduced for a single feature with no stated operational need.
- A repository interface with only one implementation and no architectural reason for the
  indirection.
- Business logic leaking into an endpoint handler instead of living in the Domain/Application layer.
- A module's Infrastructure directly referencing another module's Infrastructure.
- Tenant data touched without `ITenantDatabase.Set(...)` having been called first in that scope.

# Quality Checklist

- [ ] Module ownership and DB (catalog/tenant) are both deliberate, not incidental.
- [ ] `RowingClub.ArchitectureTests` passes.
- [ ] Cross-module needs go through MediatR/BuildingBlocks/Contracts, not a direct reference.
- [ ] No new abstraction exists without a genuine current consumer.
- [ ] Tenant-data code paths all pass through `ITenantDatabase.Set(...)`.

# Handoff

- API contract shape → `api-designer`.
- Schema/persistence design → `database-designer`.
- Auth/permission implications → `auth-security`.
- Background-job/external-call resilience → `backend-reliability`.
