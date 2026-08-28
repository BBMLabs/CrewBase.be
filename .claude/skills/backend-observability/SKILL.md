---
name: backend-observability
description: >
  Design and review production observability for this repo — structured logging (Serilog),
  metrics/traces (OpenTelemetry/Prometheus), correlation IDs, and health checks. Use when adding
  a new critical workflow, background worker, or external integration, and whenever a failure
  needs to be made diagnosable without local reproduction.
---

# Mission

Make production behavior understandable without reproducing every issue locally. This repo
already has structured logging, tracing, and metrics wired up — this skill's job is making sure a
*new* piece of work is actually covered by that pipeline, not inventing a parallel one.

# When to Use

- A new critical workflow (payment, booking, auth) that should be diagnosable in production.
- A new background worker or external integration.
- Reviewing whether an existing failure path is actually visible when it happens.

# Do Not Use

- Fixing the underlying reliability problem itself — that's `backend-reliability` (this skill
  makes a failure visible; that one makes it less likely/more recoverable).
- Diagnosing a specific live incident — that's `debugger`, using what this skill has made
  available.

# Inputs to Inspect

- `.claude/rules/04-reliability-observability.md` §Logging/Metrics/Health for this repo's exact
  setup (Serilog enrichment fields, OpenTelemetry instrumentation, the `/health` scope, and the
  known gap: no Npgsql/EF Core tracing instrumentation).
- `PerformanceBehavior`'s 500ms slow-handler warning — already-available signal, don't duplicate it.
- `ICorrelationIdAccessor`/`CorrelationIdMiddleware` for how the correlation ID already flows
  through a request.

# What already exists — don't reinvent it

- **Structured logs**: Serilog, enriched per-request with `CorrelationId`, environment name, and
  the standard ASP.NET Core request fields. The `traceId` in a ProblemDetails error response is
  the same correlation ID — that's the log lookup key, not something to build separately.
- **Traces/metrics**: OpenTelemetry, ASP.NET Core + `HttpClient` + runtime instrumentation,
  exported over OTLP when `OTEL_EXPORTER_OTLP_ENDPOINT` is set; Prometheus scrapes alongside it.
- **Slow-handler signal**: `PerformanceBehavior` logs a warning above 500ms for any MediatR
  request — this is the cheapest first check for a slow-handler regression.
- **Health**: `/health` currently checks only Postgres connectivity. Redis is deliberately
  excluded (see `backend-reliability`) since the app is designed to run with Redis down.
- **Audit trail**: `IAuditLogger` for sensitive actions (`LOGIN_*`, `COMPANY_USER_*`,
  `COMPANY_SUBSCRIPTION_*`) — a new sensitive action should extend this, not start a separate log.

# Hard Rules

## MUST

- MUST NOT log secrets — see `03-security-policy.md` §Secrets; this applies to every log line
  this skill reviews, not just ones near auth code.
- MUST give any newly-caught, meaningful exception real context in its log line (what operation,
  what entity/id where that's not itself PII, not just the exception message).
- MUST let a request-scoped log rely on the existing `CorrelationId` enrichment rather than
  inventing a parallel identifier.
- MUST NOT swallow an exception silently in a background worker — log it (see
  `backend-reliability` for the per-tenant isolation pattern) so a repeated failure is visible.

## SHOULD

- SHOULD use structured log properties (`{PropertyName}`) rather than string-interpolating
  variable data into the message template — this is what makes Serilog output queryable.
- SHOULD expose a metric for a genuinely critical new workflow (payment success/failure rate,
  queue depth) rather than relying on log-grepping alone — but don't add a metric with no
  consumer/dashboard in mind.
- SHOULD treat a query added to a hot path as untraced until Npgsql/EF instrumentation is added —
  don't assume a slow query will show up in a trace today; it currently won't.

## MAY

- MAY add a new health-check dependency to `/health` when that dependency's outage should
  actually take readiness down with it — but this is the exception (see Redis's deliberate
  exclusion), not the default for every new external dependency.

# Decision Framework

- If this fails in production at 2am, what would someone need in the log line to understand it
  without a debugger attached?
- Is this failure already covered by an existing signal (`PerformanceBehavior`, the audit log,
  standard request logging), or does it need something new?
- Does this dependency's outage mean the app is genuinely unhealthy, or just degraded (like Redis)?

# Workflow

1. Identify what could go wrong in the new workflow and what someone would need to diagnose it.
2. Check whether an existing signal already covers it (correlation ID, slow-handler log, audit
   log) before adding something new.
3. Add structured log properties with real context at the failure points that matter.
4. Decide deliberately whether this needs a new metric or health-check dependency — most new work
   doesn't.

# Anti-Patterns

- A log line with only a generic "something failed" message and no identifying context.
- String-interpolated log messages instead of structured properties.
- A silently-swallowed exception in a background worker with no log at all.
- A secret or PII value included in a log line "just for debugging."
- Assuming a slow query will show up in a trace, when Npgsql/EF instrumentation isn't wired up.

# Quality Checklist

- [ ] No secrets/PII in any new or changed log line.
- [ ] Meaningful exceptions carry real, structured context.
- [ ] No exception is silently swallowed without a log.
- [ ] A new metric/health-check dependency (if added) has a real consumer in mind.

# Handoff

- Fixing the underlying failure mode → `backend-reliability`.
- Using these signals to resolve a live incident → `debugger`.
- Query-level tracing gaps that block diagnosis → `backend-performance`/`database-designer`.
