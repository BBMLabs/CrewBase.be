# Backend Engineering Constitution

This file is the constitution, not the rulebook. It says how decisions get made and prioritized.
The actual constraints live in `.claude/rules/*.md`; the actual expertise and this repo's
concrete conventions live in `.claude/skills/*/SKILL.md` (several skills have deeper
`references/*.md` files — read those before designing anything non-trivial in that area). Read
`.claude/rules/00-orchestration.md` before starting any backend task — it decides which skills,
if any, this task needs.

```
CLAUDE.md          → orchestration, priorities, philosophy, Definition of Done
.claude/rules/     → what we always/never do (constraints)
.claude/skills/    → how we do a specific kind of work well (expertise, loaded on demand)
```

This repo (RowingClub/CrewBase.be) is a .NET 10 modular monolith: `src/BuildingBlocks/*` shared
core, `src/Modules/<Module>/{Domain,Application,Infrastructure}` per module, a single catalog
Postgres DB plus one Postgres DB per tenant company. Full architectural detail is in
`backend-architect`'s `SKILL.md` and its `references/`.

---

# Mission

Build backend systems that are:
- correct
- secure
- maintainable
- observable
- resilient
- performant
- production-ready

Prefer simple, explicit implementations over clever abstractions.

---

# Skill Orchestration

Eleven backend skills plus three shared engineering skills (`feature-planner`, `debugger`,
`code-reviewer`) exist under `.claude/skills/`. **Use the minimum number of skills necessary for
the task** — do not activate every backend skill for every backend change.

Typical order for substantial backend work:

1. `feature-planner`
2. `backend-architect`
3. `api-designer`
4. `database-designer`
5. `auth-security` — when relevant
6. `backend-reliability` — when relevant
7. implementation
8. `test-engineer`
9. `backend-performance` — when relevant
10. `backend-observability`
11. `security-reviewer`
12. `code-reviewer`
13. `migration-manager` — when applicable
14. `production-readiness`

Small changes use only the relevant subset — see `.claude/rules/00-orchestration.md` for the
full task→skill lookup table and the per-scenario pipelines (new feature, API change, DB change,
auth change, incident, performance problem, integration, release).

---

# Priority Order

When requirements conflict, use this priority:

1. Security
2. Data integrity
3. Functional correctness
4. Authorization correctness
5. Reliability
6. Backward compatibility
7. Observability
8. Performance
9. Architecture
10. Developer ergonomics
11. Elegance

Never trade security or data integrity for convenience. Concretely: if a performance optimization
would compromise data integrity, data integrity wins. If developer ergonomics would weaken
security, security wins. If an elegant abstraction would add unnecessary complexity, simplicity
wins. State which rule won when a real conflict comes up — don't resolve it silently.

---

# Backend Philosophy

Prefer:
- explicit behavior
- small cohesive modules
- predictable data flow
- strong validation
- clear failure modes
- backwards-compatible changes
- observable production behavior
- boring and proven solutions

Avoid:
- premature abstractions
- unnecessary distributed systems
- unnecessary service boundaries
- hidden side effects
- implicit authorization
- duplicated business rules
- speculative infrastructure
- technology introduced without a concrete requirement

---

# Architecture Rules

Business rules MUST NOT depend directly on transport details when practical. Infrastructure
concerns SHOULD remain separated from domain behavior. Dependencies SHOULD flow toward stable
business logic.

Do not introduce a new service, queue, cache, database, abstraction layer, provider, repository,
or framework without a concrete reason. Prefer the smallest architecture that cleanly satisfies
current requirements.

Full detail (module boundaries, the tenant-per-database model, layering rules enforced by
`RowingClub.ArchitectureTests`): `backend-architect` skill.

---

# API Rules

All external input is untrusted. Inputs MUST be validated at system boundaries. APIs MUST expose
intentional contracts. Do not leak internal exceptions, database details, secrets, or stack
traces. Breaking API changes require explicit justification and a migration strategy.

Full detail: `.claude/rules/02-api-data-contracts.md`, `api-designer` skill.

---

# Database Rules

Data integrity belongs primarily in the database when practical. Use appropriate constraints,
foreign keys, unique indexes, transactions, and indexes. Do not rely exclusively on application
code for critical invariants.

Full detail: `database-designer` skill.

---

# Security Rules

Authentication and authorization are separate concerns. Every protected operation MUST explicitly
enforce authorization. Never trust client-provided roles, client-provided ownership, hidden form
fields, URL IDs, or frontend validation. Secrets MUST NOT be committed to source control.

Full detail: `.claude/rules/03-security-policy.md`, `auth-security` and `security-reviewer` skills.

---

# Reliability Rules

All external dependencies may fail. Network calls MUST have bounded behavior. Consider timeouts,
retries, idempotency, backoff, concurrency, and partial failures. Retries MUST NOT blindly
duplicate non-idempotent operations.

Full detail: `.claude/rules/04-reliability-observability.md`, `backend-reliability` skill.

---

# Observability Rules

Production-critical workflows SHOULD provide sufficient structured logs, metrics, traces where
useful, health information, and error context. Never log secrets or sensitive authentication
material.

Full detail: `.claude/rules/04-reliability-observability.md`, `backend-observability` skill.

---

# Testing Rules

Test behavior and contracts rather than implementation details. Critical business rules MUST have
automated coverage. Important integration boundaries SHOULD have integration tests. Bug fixes
SHOULD include a regression test when practical.

Full detail: `.claude/rules/05-testing-policy.md`, `test-engineer` skill.

---

# Performance Rules

Measure before performing complex optimization. Database queries, remote calls, and serialization
boundaries SHOULD be reviewed for expensive behavior. Avoid obvious N+1 queries, unbounded reads,
unbounded concurrency, unnecessary network round trips, and excessive payloads.

Full detail: `backend-performance` skill.

---

# Migration Rules

Production migrations MUST consider backward compatibility, rollout order, rollback strategy,
lock duration, large tables, data backfills, and old application versions still running.
Destructive migrations MUST NOT be bundled blindly with application changes.

Full detail: `migration-manager` skill.

---

# Definition of Done

A substantial backend task is complete when:

- ✓ functionality is correct
- ✓ inputs are validated
- ✓ authorization is correct
- ✓ data integrity is preserved
- ✓ transactions are correct where needed
- ✓ error behavior is intentional
- ✓ external calls have bounded behavior
- ✓ retry behavior is safe
- ✓ large collections are bounded
- ✓ obvious N+1 queries do not exist
- ✓ relevant indexes were considered
- ✓ critical workflows are observable
- ✓ secrets are not logged
- ✓ relevant tests pass
- ✓ regression tests exist for important bug fixes
- ✓ migration is safe if schema changed
- ✓ deployment ordering is safe
- ✓ rollback/recovery was considered
- ✓ no unnecessary dependency was introduced
- ✓ no unnecessary abstraction was introduced
- ✓ security implications were reviewed
- ✓ production implications were reviewed
