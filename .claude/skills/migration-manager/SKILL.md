---
name: migration-manager
description: >
  Generate, validate, and apply EF Core migrations for this repo's two separate migration chains
  (catalog and tenant). Use whenever the schema designed by `database-designer` needs to actually
  become a migration, and whenever reviewing whether a migration is safe to run against
  already-populated production databases.
---

# Mission

Change production data safely, on a repo that has two independent migration chains and
potentially hundreds of already-populated tenant databases that every schema change must run
against cleanly.

# When to Use

- After `database-designer` has designed a schema change, to generate the actual migration.
- Reviewing a generated migration before it ships.
- Diagnosing a migration that failed to apply.

# Do Not Use

- Deciding the schema shape itself — that's `database-designer`; this skill implements and
  safety-checks what that skill decided.

# Inputs to Inspect

- `scripts/validate-migration.sh` — builds the solution, checks for pending model changes not yet
  captured in a migration, and flags `Drop(Table|Column)` operations in the latest migration for
  each context (note: it flags `Down()` method drops too — that's expected and safe; read the
  actual diff, don't just react to the script's raw match).

# Two chains

| | Catalog | Tenant |
|---|---|---|
| Context | `RowingClubDbContext` | `TenantDbContext` |
| Project (`-p`) | `src/BuildingBlocks/RowingClub.BuildingBlocks.Infrastructure` | `src/Modules/Scheduling/RowingClub.Scheduling.Infrastructure` |
| Output (`-o`) | `Postgres/Migrations` | `Persistence/Migrations` |
| Applied | at startup, `EfMigrationHostedService` | at startup, `TenantMigrationHostedService`, plus per-tenant at provisioning time |

Command template (startup project is always `src/RowingClub.Api`):
```bash
dotnet ef migrations add <Name> -p <project> -s src/RowingClub.Api -o <output> --context <Context>
```

`TenantDbContext` comes up at design-time via `TenantDbContextDesignTimeFactory` guarded by
`EF.IsDesignTime` — don't disturb that guard, the design-time factory intentionally never opens a
real connection.

# Hard Rules

## MUST

- MUST read the generated migration file before shipping it — check for an unexpected drop/alter,
  not just trust the tool.
- MUST add a default value + backfill SQL when adding a NOT NULL column to a table that already
  has rows in every existing catalog/tenant DB — see the `AddCompanyTenantDatabase` migration's
  `migrationBuilder.Sql(...)` for the pattern. This applies to every enum column too: EF silently
  picks `defaultValue: ""` if you don't specify one, which is not a valid enum value and breaks
  the next read of any pre-existing row.
- MUST resolve any potential uniqueness conflict via a backfill before adding a new unique index
  to a populated table.
- MUST run `scripts/validate-migration.sh` (build + pending-model-change check + drop-operation
  flag) before considering a migration done.
- MUST consider backward compatibility, rollout order, lock duration, and old application
  versions still running — see `04-reliability-observability.md`'s reliability rules extended to
  schema changes.

## MUST NOT

- MUST NOT use `EnsureCreated` anywhere on the tenant chain — schema only advances through
  migrations; `EnsureCreated` breaks the migration history the provisioner depends on.
- MUST NOT bundle a destructive migration blindly with an application change — a destructive
  change needs its own reasoning about deployment ordering (expand/contract, see below).
- MUST NOT hand-edit a migration file without re-checking it against the Designer/snapshot with
  `dotnet build` + `scripts/validate-migration.sh` afterward.

## SHOULD

- SHOULD prefer an expand/contract sequence for anything that would otherwise be a breaking
  schema change: add the new structure → deploy compatible code → backfill → switch reads/writes
  → verify → remove the legacy structure in a later release.

## MAY

- MAY use `dotnet ef migrations remove` to undo a migration that has not yet been applied to any
  database. Once it's been applied anywhere, write a new corrective migration instead — never roll
  back a migration that a real database has already run.

# Decision Framework

- Is the target table already populated in every existing catalog/tenant DB? If yes, does the new
  column need a default + backfill?
- Does this migration touch a column that needs to stay valid for an old, still-running
  application version during rollout?
- Is this destructive? If yes, can it be deferred to a later release after the dependent code is
  fully removed (expand/contract)?

# Workflow

1. Confirm the schema design (from `database-designer`) and which chain it belongs to.
2. Generate the migration with the command template above.
3. Read the generated file in full — Up and Down.
4. Add default/backfill SQL for any NOT NULL/enum column added to a populated table.
5. Run `scripts/validate-migration.sh`.
6. For a destructive change, plan the expand/contract sequence explicitly rather than shipping it
   in one step.

# Anti-Patterns

- A NOT NULL column added to a populated table with no default — breaks on every existing
  catalog/tenant DB the instant the migration runs.
- An enum column with EF's auto-assigned `defaultValue: ""` left unexamined.
- `EnsureCreated` used to sidestep a missing tenant migration.
- A rename-and-deploy done as a single atomic change with no compatibility window for
  already-running instances.
- A large synchronous backfill run inline in the migration with no consideration of lock duration
  on a large table.

# Quality Checklist

- [ ] Generated migration file was read in full, not just trusted.
- [ ] Any NOT NULL/enum column added to a populated table has a default + backfill.
- [ ] `scripts/validate-migration.sh` passes.
- [ ] Destructive changes have an explicit expand/contract plan, not a single bundled step.
- [ ] Rollback story is explicit (not-yet-applied → `migrations remove`; already-applied → new
  corrective migration).

# Handoff

- Schema design itself → `database-designer`.
- Production deployment ordering / rollback readiness → `production-readiness`.
- Query-shape performance impact of a new column/index → `backend-performance`.
