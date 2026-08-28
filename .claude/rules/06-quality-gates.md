# Backend Quality Gates

A substantial change should be reviewed against the applicable gates below before it's called
done. A trivial change (a copy fix in an error message, a config value tweak) only needs the
gates that are actually relevant — see `00-orchestration.md`'s activation matrix for what a given
task genuinely calls for.

## Correctness Gate

- Requirements implemented as specified.
- Edge cases handled (empty input, boundary values, null/missing optional fields).
- Error states handled explicitly, not left to bubble up as an unhandled 500.

## Data Gate

- Schema valid (see `database-designer`).
- Constraints adequate (`NOT NULL`/`UNIQUE`/`FOREIGN KEY`/`CHECK` used where they express a real
  invariant — see `02-api-data-contracts.md` §Data Integrity).
- Transactions correct — tenant-writing handlers call `ISchedulingUnitOfWork.SaveChangesAsync`
  (see `01-backend-principles.md` §Side Effects).
- Migration safe (see `migration-manager` and `02-api-data-contracts.md`; a NOT NULL column added
  to an already-populated table needs a default + backfill, every time).

## Security Gate

- Authentication correct.
- Authorization correct — both the endpoint-level role check AND the handler-level server-side
  re-verification (see `03-security-policy.md` §Authorization).
- Inputs validated at the boundary.
- Secrets protected (never logged, never returned in a response).
- Exposed attack surface reviewed for the specific change (see `security-reviewer`).

## Reliability Gate

- Dependency failure considered (see `04-reliability-observability.md`).
- Timeouts configured for any new outbound call.
- Retry behavior safe (idempotent operation, or no blind retry).
- Duplicate processing considered for anything webhook/queue-driven.

## Performance Gate

- No obvious N+1 (missing `Include`, or a loop making one DB call per iteration).
- Collection reads bounded — no unbounded `GetAllAsync`-style call added to a path that can grow
  large without pagination.
- Query indexes considered for a new/changed query shape (see `database-designer`'s
  `references/indexing.md`).
- Network calls bounded (no unbounded fan-out of outbound requests).
- Concurrency bounded (no unbounded parallel work spawned per request/tick).

## Observability Gate

- Important failures are observable (a caught exception that matters is logged with context, not
  swallowed silently).
- Logs contain sufficient context (correlation ID implicitly present via Serilog enrichment —
  don't fight it by logging outside the normal pipeline).
- No secrets logged.

## Testing Gate

- Relevant automated tests exist (see `05-testing-policy.md` for which project/layer).
- A regression test was added for a bug fix when practical.
- If an endpoint was added/removed, the Security/Functional test inventories were updated.

## Release Gate

- Environment configuration documented (new env var added to `.env.example` and — critically —
  to `docker-compose.yml`; a var that exists only in `.env.production` but not compose has broken
  production before in this project).
- Migrations safe for a live, populated database.
- Rollout/deployment ordering safe.
- Rollback possible, or an explicit reason it isn't.
