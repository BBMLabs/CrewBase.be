---
name: backend-reliability
description: >
  Design resilient behavior for remote calls, background workers, retries, timeouts, idempotency,
  and partial failures in this repo. Use for anything touching an outbound HTTP call (iyzico,
  reCAPTCHA, SMTP), a background worker, or a webhook/queue-shaped consumer.
---

# Mission

Make expected failures predictable and recoverable. This repo has real external dependencies
(Postgres, Redis, SMTP, iyzico, reCAPTCHA, an OTLP collector) and real background workers — this
skill's job is making sure a failure in any of them degrades gracefully instead of corrupting
state or taking the whole app down.

# When to Use

- Any new or changed outbound `HttpClient` call.
- Any new background worker (`BackgroundService`).
- Any webhook consumer or anything that could receive duplicate delivery.
- Reviewing whether an existing integration needs a retry/timeout/idempotency story it doesn't
  have yet.

# Do Not Use

- Pure architecture decisions with no failure-mode dimension — that's `backend-architect`.
- Diagnosing an incident that already happened — that's `debugger`, which then may hand back here
  to fix the underlying reliability gap.

# Inputs to Inspect

- `.claude/rules/04-reliability-observability.md` — the concrete inventory of this repo's
  external dependencies, background workers, and the **known gap**: no Polly/circuit-breaker
  policy exists on any outbound `HttpClient` today.
- `IIdempotencyStore`/`IdempotencyBehavior`/`RedisIdempotencyStore` for the client-driven
  idempotency-key mechanism.
- `ReminderWorker`/`SubscriptionSafetyNetWorker` for the two existing background-worker patterns.

# Hard Rules

## MUST

- MUST assume every network dependency (Postgres, Redis, SMTP, iyzico, reCAPTCHA, OTLP) can fail,
  time out, or return malformed/slow data.
- MUST bound every remote operation with a timeout — an unbounded wait on an external call is not
  acceptable.
- MUST evaluate idempotency before adding any retry — see `02-api-data-contracts.md`
  §Idempotency for the two mechanisms already available (`IIdempotentCommand`, payment-reference
  dedup). Use one of them rather than assuming at-most-once delivery.
- MUST give a new per-tenant background job its own DI scope per tenant and isolate one tenant's
  failure from the rest — copy `ReminderWorker`'s pattern (new `AsyncScope` per company, try/catch
  around each company's work, loop continues on failure).
- MUST NOT let a background worker's per-tick exception kill the worker itself — catch, log, wait
  for the next tick (both existing workers already do this; preserve it in any new one).

## MUST NOT

- MUST NOT blindly retry a non-idempotent operation.
- MUST NOT retry a permanently invalid request (a 4xx from well-formed-but-rejected input).
- MUST NOT assume exactly-once delivery for anything webhook- or queue-shaped.
- MUST NOT let Redis being down take the whole app down — the existing idempotency store is
  configured `AbortOnConnectFail=false` specifically so a Redis outage only disables idempotency
  caching, not the app; preserve that property for anything new that touches Redis.

## SHOULD

- SHOULD use exponential backoff where a retry is genuinely warranted.
- SHOULD add a real resilience policy (Polly or equivalent) to a new/changed outbound `HttpClient`
  call rather than assuming the framework already provides one — it currently doesn't, for any
  client in this repo.
- SHOULD define an explicit recovery strategy for async failures (dead-letter/poison-message
  handling, or at minimum a clear log + alert path) rather than a silent swallow.

## MAY

- MAY defer adding full circuit-breaking to a call that isn't yet a proven pain point — but flag
  the gap explicitly rather than silently assuming coverage that doesn't exist.

# Decision Framework

- Can this operation be safely repeated? If not, does it need an idempotency key or a
  dedup-by-reference check before it can be retried at all?
- What should happen to the caller/worker when this dependency is down — degrade, queue, or fail
  loudly?
- Is this per-tenant work? If yes, does it get its own scope and isolated failure handling like
  `ReminderWorker`?
- Does a duplicate delivery of this exact call corrupt anything (double-charge, double-create)?

# Workflow

1. Identify what can fail and how (timeout, malformed response, duplicate delivery).
2. Decide the idempotency story before deciding the retry story.
3. Bound the call with a timeout; add backoff/retry only where genuinely safe.
4. For a new background worker, follow the per-tenant-scope-and-isolated-failure pattern.
5. Hand off to `backend-observability` so the failure mode is actually visible in production.

# Anti-Patterns

- A payment/webhook handler retried without checking whether it already ran.
- A background worker where one tenant's exception stops the whole tour.
- An outbound HTTP call with no timeout, able to hang the request indefinitely.
- Assuming a resilience policy exists on an `HttpClient` just because the pattern would be
  reasonable — verify, this repo currently has none configured.

# Quality Checklist

- [ ] Every new/changed outbound call has a bounded timeout.
- [ ] Idempotency is addressed before any retry logic is added.
- [ ] A new per-tenant background job isolates each tenant's failure.
- [ ] A dependency outage degrades gracefully rather than taking the app down.
- [ ] The failure mode is observable (see `backend-observability`), not silent.

# Handoff

- Making the failure mode visible in production → `backend-observability`.
- Test coverage for retry/duplicate-delivery behavior → `test-engineer`.
- Diagnosing a live incident → `debugger`.
