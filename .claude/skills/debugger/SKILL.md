---
name: debugger
description: >
  Diagnose a failure in this repo using evidence-driven root cause analysis — where to look in
  logs/traces, this repo's typical failure patterns, and the end-to-end verification method. Not
  backend-specific in principle, but this file's evidence sources and failure catalog are
  CrewBase.be's own.
---

# Mission

Identify root cause before changing code. Symptom-driven random changes are the failure mode this
skill exists to prevent.

# When to Use

- Any reported bug, exception, or unexpected behavior.
- A production incident (paired with `backend-observability` for what signals exist).

# Do Not Use

- Designing a fix for a well-understood, already-diagnosed problem — once root cause is found,
  hand off to the relevant domain skill (`backend-architect`/`auth-security`/etc.) to design the
  actual fix.

# Evidence sources in this repo

- Serilog console output — every request carries a `CorrelationId`; match it against the
  `traceId` in the error response to find the exact log line.
- The ProblemDetails `code` field — names the error class; cross-reference
  `api-designer`'s `references/error-format.md` dictionary.
- Audit logs — `AUDIT [EVENT]` lines (login, user management actions).
- `/health` — Postgres connectivity; Prometheus metrics and OpenTelemetry traces are also live.

# Typical failure patterns in this repo

1. **"Tenant database was not resolved for this request"** — a code path reached a tenant
   repository without `ITenantDatabase.Set(...)` having run first. Check for a missing resolver
   call at the endpoint, or a scope mix-up in a background job (a new per-tenant scope is
   required per company — see the `ReminderWorker` pattern in `backend-reliability`).
2. **Migration fails at startup** — which chain? Catalog: `EfMigrationHostedService`. Tenant:
   `TenantMigrationHostedService` (logs are per-company). If a tenant DB was left without a
   migration history (an old `EnsureCreated` relic), it needs to be dropped and re-provisioned.
3. **A search against an encrypted column returns nothing** — a LINQ `Where` was written against
   the plaintext-looking column directly; it must go through the blind index instead
   (`CustomerRepository.GetByPhoneAsync` is the reference).
4. **Unexpected 401/403** — is a JWT claim missing (role/companyId), or is the handler's own DB
   re-verification the thing rejecting it? These are two different layers — check both
   separately (see `auth-security`).
5. **Corrupted Turkish-character JSON** — usually a UTF-8 issue in `curl`; write the payload to a
   file and use `--data-binary @file` instead of an inline `-d` string.

# Hard Rules

## MUST

- MUST distinguish symptoms from root cause — "the request returns 500" is a symptom, not a
  diagnosis.
- MUST prefer evidence (logs, traceId, a reproduced failure) over intuition about what's probably
  wrong.
- MUST reproduce the failure (curl/test) before claiming a fix resolves it.

## MUST NOT

- MUST NOT make speculative code changes without a hypothesis tied to actual evidence.
- MUST NOT declare a fix complete without re-running the same reproduction that originally showed
  the failure.

## SHOULD

- SHOULD add regression protection (see `test-engineer`) once root cause and fix are confirmed.

# Decision Framework

- What's the exact reproduction (request, input, timing)?
- What does the correlation-ID-matched log line actually say?
- Does the failure match one of the catalog patterns above?
- What's the single smallest hypothesis that explains the evidence — test that one change, not
  several at once.

# Workflow

1. Reproduce the failure (start the app with `dotnet run` in the background, wait for `/health`,
   drive the flow with curl).
2. Gather evidence: find the log line via `traceId`/`CorrelationId`.
3. Narrow scope: which layer, which module.
4. Form a specific hypothesis; check it against the failure-pattern catalog above.
5. Test the hypothesis with the smallest possible change.
6. Confirm root cause, apply the smallest fix.
7. Re-run `dotnet test` plus the original curl reproduction.
8. Hand off to `test-engineer` for regression coverage.

# Anti-Patterns

- Changing several things at once "to see what fixes it."
- Treating a caught exception's surface message as root cause without checking what's underneath.
- Declaring victory without re-running the original reproduction.

# Quality Checklist

- [ ] Root cause identified with evidence, not assumed.
- [ ] Fix is the smallest change that addresses the actual cause.
- [ ] Original reproduction was re-run and now passes.
- [ ] `dotnet test` passes after the fix.
- [ ] Regression test added where practical.

# Handoff

- The actual fix design, once root cause is known → the relevant domain skill
  (`backend-architect`/`auth-security`/`database-designer`/`backend-reliability`).
- Regression coverage → `test-engineer`.
- If the incident revealed an observability gap → `backend-observability`.
