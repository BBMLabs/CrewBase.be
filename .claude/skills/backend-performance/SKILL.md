---
name: backend-performance
description: >
  Analyze and improve backend latency, throughput, EF Core query behavior, connection pool usage,
  and background-job cost in this repo. Use for a reported slow endpoint, a performance review of
  changed code, or before adding a query/loop that could scale badly. Measure before optimizing —
  observe via `backend-observability` first when the bottleneck isn't already known.
---

# Mission

Improve measurable backend performance without compromising correctness. Identify the actual
bottleneck before making a change — this repo has specific, known cost centers (below); don't
guess at a generic optimization when one of these is more likely the real cause.

# When to Use

- A reported slow endpoint or background job.
- Reviewing new/changed code that adds a database query, a loop with I/O inside it, or new
  background-job work.
- Before adding an index (confirm the query pattern first — see `database-designer`).

# Do Not Use

- Schema/index design itself — that's `database-designer` (this skill flags the need; that skill
  designs the fix).
- Diagnosing a production incident with no clear performance signal yet — start with `debugger`.

# Inputs to Inspect

- `PerformanceBehavior`'s 500ms "Yavaş işlem" log — the first, cheapest signal for a slow MediatR
  handler; check it before profiling from scratch.
- The four cost centers below, all specific to this codebase.
- Whether the query in question touches an encrypted PII column (see next section).

# This repo's known cost centers

1. **Encrypted columns.** Server-side filter/sort on `FullName`/`Phone`/`Email` is impossible —
   these tables get `ToListAsync`'d and processed in memory. Any list that can grow (customers,
   `appointments` `GetAll`) needs pagination added before it's allowed to grow unbounded; don't
   let a `GetAllAsync` call on one of these tables ship without that in mind.
2. **No per-tenant `NpgsqlDataSource`.** Tenant connections are opened by connection string;
   Npgsql pools per connection string, which is fine today but worth revisiting
   (`Maximum Pool Size`) if the tenant count grows into the thousands.
3. **`ReminderWorker`.** Walks every active company every minute, opening a scope + DB connection
   per company. If company count grows, tune the scan interval/parallelism, and keep
   `GetPendingRemindersAsync` bounded by its date window rather than widening it.
4. **Availability queries.** Day sessions + boats currently come back in one query, with slot
   iteration done in memory — this is intentionally cheap. Preserve `Include` chains when adding
   a related query here; don't introduce N+1 by accident.

# Hard Rules

## MUST

- MUST identify the actual bottleneck (via `PerformanceBehavior` logs, Prometheus, or a direct
  measurement) before making a non-trivial optimization — see `04-reliability-observability.md`'s
  observe-then-measure-then-optimize order.
- MUST NOT trade correctness for speed.
- MUST review any newly-added expensive database access (a loop containing an `await` DB call,
  a missing `Include` causing lazy-load/N+1, a full-table read where a `CountAsync`/`AnyAsync`
  would do).
- MUST bound large reads and concurrency — a new endpoint that can plausibly return an unbounded
  result set needs pagination considered explicitly, not assumed away.

## MUST NOT

- MUST NOT add caching as a default first response to a slow path — see `backend-reliability`
  and `03-security-policy.md` for what's safe to cache (never PII, never without an invalidation
  story).
- MUST NOT add an index without a real, current query pattern (see `database-designer`).
- MUST NOT optimize a query that has no evidence of being slow.

## SHOULD

- SHOULD prefer eliminating unnecessary work (a redundant round trip, a query that shouldn't run
  at all under the actual condition) over optimizing work that shouldn't be happening in the
  first place.
- SHOULD respect background-job cancellation tokens; a hung job should be cancellable without
  killing the hosting service.

## MAY

- MAY defer a performance fix for a path with no current evidence of being a bottleneck — flag it
  as a known limitation instead of speculatively "fixing" it.

# Decision Framework

- Is there a measured signal (the 500ms handler log, a Prometheus metric, a user report with
  numbers), or is this a guess? Get the signal first.
- Is the slow path one of the four known cost centers above, or something new?
- Would eliminating the work (skip an unnecessary call, narrow a query) solve it more simply than
  optimizing it?
- Does the fix risk correctness (a cache that can go stale, a batched write that changes ordering
  guarantees)?

# Workflow

1. Get a real measurement — `PerformanceBehavior` log, Prometheus, or a direct repro.
2. Check whether it matches one of the four known cost centers.
3. Look for missing `Include`/N+1, unbounded reads, or in-loop `await`s in the changed code.
4. Fix the smallest thing that removes the measured cost; re-measure.
5. Hand off to `database-designer` if the fix implies an index/schema change.

# Anti-Patterns

- Adding a cache before establishing there's a real, repeated cost to avoid.
- An index added on intuition, with no query plan or access pattern behind it.
- A `foreach` loop making one DB round trip per iteration where a single batched query would do.
- Optimizing a path nobody has shown to be slow, at the cost of code clarity.

# Quality Checklist

- [ ] A real bottleneck was identified before optimizing, not assumed.
- [ ] No new N+1 was introduced (`Include` chains preserved/extended correctly).
- [ ] Any newly-unbounded list has a pagination plan or a documented reason it's safe without one.
- [ ] A background job's cancellation-token/failure-isolation behavior wasn't weakened.

# Handoff

- Index/schema implications → `database-designer`.
- Making the cost visible for next time → `backend-observability`.
- A live incident with no clear cause yet → `debugger`.
