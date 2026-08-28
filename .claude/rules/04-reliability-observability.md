# Reliability and Observability

## External Dependencies

Assume every network dependency can fail, timeout, return malformed data, become slow, rate
limit, or duplicate a response. This repo's external dependencies: Postgres, Redis (idempotency
store), SMTP (Brevo), iyzico (two separate APIs — Subscription and classic CheckoutForm),
reCAPTCHA, and the OTLP collector.

**Known gap:** no Polly (or equivalent) retry/circuit-breaker policy is currently wired onto any
outbound `HttpClient` (iyzico clients, `GoogleRecaptchaVerifier`). A transient failure on these
calls today surfaces as a plain exception, not a retry. Treat adding resilience to a given
outbound call as in scope whenever `backend-reliability` touches that call — don't assume it
already exists because the pattern would be reasonable.

---

## Timeouts

Remote operations MUST have bounded timeouts. Never allow uncontrolled indefinite waits.

---

## Retries

Retries MUST be intentional. Use backoff when appropriate. Do not retry permanently invalid
requests (4xx from a well-formed request). Do not blindly retry non-idempotent operations —
before adding a retry, confirm the operation is safe to duplicate, or make it idempotent first
(see `02-api-data-contracts.md` §Idempotency).

---

## Async Processing

Consumers SHOULD tolerate duplicate delivery when the system can produce it. Failures SHOULD have
an explicit recovery strategy.

This repo's background workers, and the pattern each follows:

- **`ReminderWorker`** (per-minute) — lists all active companies, then opens a **new DI scope per
  company** and sets `ITenantDatabase` in that scope before touching tenant data. Each company's
  work is wrapped in its own try/catch so one company's failure doesn't stop the tour for the
  rest — copy this per-tenant isolation pattern for any new per-tenant background job.
- **`SubscriptionSafetyNetWorker`** (hourly) — applies overdue plan downgrades. This is the
  **primary** mechanism for applying a downgrade, not a fallback; the iyzico webhook is the
  fallback that catches a missed run. Only touches the catalog DB, so (unlike `ReminderWorker`) it
  does not need a per-company scope.
- **`EfMigrationHostedService`** / **`TenantMigrationHostedService`** — catalog and per-tenant
  migration application at startup; tenant migration is isolated per company (one company's
  migration failure is logged, not fatal to the others).

Consider: retry policy, dead-letter handling, poison messages, idempotency — none of these exist
today beyond what's listed above; adding one for a new async path is a real design decision, not
a checkbox.

---

## Idempotency (for async/webhook-driven work)

See `02-api-data-contracts.md` §Idempotency for the two concrete mechanisms already in this repo
(`IIdempotentCommand`/`IdempotencyBehavior`, and payment-reference dedup). A new webhook consumer
or queue handler MUST use one of these rather than assuming at-most-once delivery.

---

## Logging

Prefer structured logs. This repo uses Serilog (`SerilogSetup`) with per-request enrichment:
`CorrelationId` (from `CorrelationIdMiddleware`/`ICorrelationIdAccessor`), environment name, and
the standard ASP.NET Core request fields (endpoint, method, status code, duration). The
`traceId` returned in a ProblemDetails error response is the same correlation ID — use it to find
the matching log line.

Never log sensitive secrets — see `03-security-policy.md` §Secrets.

---

## Metrics

Critical workflows SHOULD expose useful metrics. This repo exports via OpenTelemetry
(`OpenTelemetrySetup`) — ASP.NET Core instrumentation, `HttpClient` instrumentation, and .NET
runtime instrumentation, all shipped over OTLP to `OTEL_EXPORTER_OTLP_ENDPOINT` when configured;
Prometheus is scraped alongside it. `PerformanceBehavior` additionally logs a warning for any
MediatR request that exceeds **500ms** ("Yavaş işlem") — this is the first place to look for a
slow-handler regression before reaching for a full trace.

**No Npgsql/EF Core OpenTelemetry instrumentation is wired up** — query-level spans don't exist
yet; don't assume a trace will show a slow query, it currently only shows the outer request/handler.

---

## Health

Health checks SHOULD distinguish process health from dependency readiness. This repo's `/health`
currently checks Postgres connectivity (`PostgresHealthCheck`); Redis is deliberately **not** a
health-check dependency — the app is designed to stay up with Redis down
(`AbortOnConnectFail=false`), since Redis here only backs the idempotency cache, not core
functionality.
