---
name: database-designer
description: >
  Design and review this repo's relational schema — the catalog/tenant DB split, encrypted-column
  rules, indexing, transaction boundaries, and EF Core mapping patterns. Use for any schema
  design or change, before writing a migration.
---

# Mission

Protect data integrity while supporting the repo's actual access patterns — across two distinct
schema worlds that must not be confused.

# When to Use

- Designing a new entity/table or changing an existing one.
- Adding a column, especially PII or a searchable field.
- Deciding an index, constraint, or transaction boundary.
- Before any `migration-manager` work — schema design comes first, the migration implements it.

# Do Not Use

- Deciding *whether* a migration is safe to ship to production data — that's `migration-manager`.
- Query-level performance tuning with no schema change — that's `backend-performance`.

# Inputs to Inspect

- `references/database-rules.md` — connection/naming/soft-delete/FK-behavior/concurrency rules.
- `references/indexing.md` — the existing index inventory and the reasoning behind each one; add
  new indexes with the same reasoning, not by habit.
- Which world the entity belongs in (see below) before writing a single line of EF config.

# Two schema worlds — pick one first

**Catalog DB** (`rowingclub`, `RowingClubDbContext`): companies (`identity_companies`), users
(`identity_users`), identity/session tables, outbox/inbox. Migrations under
`BuildingBlocks.Infrastructure/Postgres/Migrations`. Aggregates use `HasAggregateRootDefaults()`
(Id + `Version` optimistic-concurrency token).

**Tenant DBs** (one per company, `tenant_<slug>`, `TenantDbContext`): `customers`,
`appointments`, `training_sessions`, `boats`, `instructors`, `lesson_packages`,
`company_settings`. Migrations under `Scheduling.Infrastructure/Persistence/Migrations`. Company
identity is never written into these tables — isolation is at the database level, not a column.

# Hard Rules

## MUST

- MUST identify which DB (catalog vs. tenant) an entity belongs to before designing it.
- MUST encrypt PII (name, phone, email, notes, health info, etc.) stored in a tenant DB as an
  AES-GCM `text` column via the existing converter pattern in `TenantDbContext`. If it needs to
  be searchable, add a blind-index column (`PhoneIndex` pattern) and populate it in the
  `SaveChangesAsync` override — a SQL `WHERE`/`ORDER BY` cannot run against the encrypted column
  itself.
- MUST store enums as strings: `.HasConversion<string>().HasMaxLength(...)`, with an explicit
  `.HasDefaultValue(...)` whenever the migration adds this column to an already-populated table
  (see `migration-manager`).
- MUST use `decimal` + `.HasPrecision(12,2)` for money, `DateOnly` for dates, `TimeOnly` for
  times, `DateTimeOffset` for timestamps (named `*AtUtc`).
- MUST enforce uniqueness in the database via a unique index — a "unique among active rows" rule
  uses a filtered index (`.HasFilter("\"Status\" <> 'Cancelled'")`), not an application-only check
  (which loses a race).
- MUST advance tenant schema only through migrations — `EnsureCreated` on the tenant chain is
  forbidden, it breaks the migration history the provisioner relies on.

## MUST NOT

- MUST NOT write company/tenant identity into a tenant-DB table's columns — that's what the
  separate database already provides.
- MUST NOT add an index without a real query pattern behind it — tenant DBs are small; an
  unjustified index is pure write-cost with no benefit.
- MUST NOT rely on an encrypted column for filtering/sorting server-side.

## SHOULD

- SHOULD prefer `IsActive`/`Status` flags over hard delete; physical deletion is reserved for
  genuine cascade relationships (e.g. appointment → customer/session).
- SHOULD choose FK behavior deliberately: cascade where the child record loses meaning without
  the parent (member deleted → their appointments go too), `SetNull` where it shouldn't (boat/
  instructor deleted → the session stays).
- SHOULD seed a single-row settings table (`company_settings`) during tenant provisioning, and
  keep a `?? CompanySettings.Default()` fallback on read for defensiveness.

## MAY

- MAY denormalize a value at write time (e.g. package name/price snapshotted onto
  `CustomerPackage` at assignment) when history must survive the source record changing later —
  document why in a comment, since it's the exception, not the default.

# Decision Framework

- Catalog or tenant DB?
- Does this column carry PII? If yes: encrypted, and does it need a blind index?
- What's the real invariant here, and can the database enforce it directly (constraint/unique
  index) instead of trusting application code?
- What's the actual query pattern this needs to serve — index for that, not for "might be useful"?

# Workflow

1. Determine catalog vs. tenant DB for the entity.
2. Design columns with the right EF Core primitive/converter per Hard Rules above.
3. Identify real invariants and back them with a DB constraint.
4. Identify real query patterns and index for those specifically (`references/indexing.md`).
5. Hand off to `migration-manager` to generate and safety-check the actual migration.

# Anti-Patterns

- A plaintext PII column in a tenant DB.
- An index added "just in case" with no query it serves.
- A uniqueness rule enforced only by an application-level check-then-insert (races under
  concurrent requests).
- `EnsureCreated` used on the tenant chain to "just get it working."
- Money stored as `float`/`double` instead of `decimal`.

# Quality Checklist

- [ ] Catalog vs. tenant DB choice is correct and deliberate.
- [ ] PII is encrypted; searchable PII has a blind-index column.
- [ ] Enum columns have an explicit default when added to a populated table.
- [ ] Uniqueness/critical invariants are enforced by a DB constraint, not just app code.
- [ ] Every new index maps to a real, current query pattern.

# Handoff

- Turning the design into a real migration → `migration-manager`.
- Query-shape performance concerns → `backend-performance`.
- API DTO shape exposing this data → `api-designer`.
