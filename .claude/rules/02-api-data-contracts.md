---
paths:
  - "src/RowingClub.Api/**/*.cs"
  - "src/Modules/**/*.cs"
---

# API and Data Contract Rules

## Input Boundaries

Every external input MUST be considered untrusted. Validate:
- body
- query
- path parameters
- headers
- uploaded files
- webhook payloads
- queue messages

Dates/times arrive as strings (`yyyy-MM-dd`, `HH:mm`) and are parsed at the endpoint; a parse
failure returns 400 with `invalid_date`/`invalid_time` — see `api-designer`'s
`references/rest-conventions.md`.

---

## API Contracts

Endpoints MUST have intentional request and response contracts. Avoid leaking persistence models
directly — Domain entities MUST NOT cross the API boundary; map to a DTO defined in the
Application layer.

This repo's concrete conventions (route shape, verbs, versioning, pagination-by-query,
soft-deactivation over hard delete) are in `api-designer`'s `references/rest-conventions.md` —
follow them rather than inventing a parallel convention for a new endpoint.

---

## Error Contracts

Errors SHOULD be predictable, machine-readable where appropriate, and useful without exposing
internals. Never expose raw stack traces in production responses.

This repo has exactly two error shapes — see `api-designer`'s `references/error-format.md` for
the full mapping and the reusable error-code dictionary:
1. Endpoint-level early return: `ApiResponse.Fail(code, message)` + explicit HTTP status.
2. Handler-thrown exception → `GlobalExceptionHandler` → RFC 7807 ProblemDetails (`DomainException`
   → 409, `NotFoundException` → 404, `AuthenticationFailedException` → 401, FluentValidation → 400
   `validation_error`, unexpected → 500 `unexpected_error`).

Reuse an existing error code before adding a new one.

---

## Pagination

Potentially large collections MUST be bounded. Prefer cursor pagination when offset pagination
becomes unsafe or expensive; today most list endpoints here are small enough for plain
`?page=&pageSize=` query pagination, but see `backend-performance`'s note on `GetAllAsync` calls
over encrypted-PII tables that can't be filtered/sorted server-side.

---

## Idempotency

Operations vulnerable to duplicate delivery SHOULD consider idempotency — particularly payments,
webhooks, queue consumers, and external callbacks.

This repo has two real mechanisms, use the one that fits:
1. **`IIdempotentCommand` + `IdempotencyBehavior`** (Redis-backed, `RedisIdempotencyStore`,
   24h TTL) — for client-driven `Idempotency-Key` requests. Opt in by implementing
   `IIdempotentCommand` on the command.
2. **Payment-reference dedup** — for provider-driven duplicate delivery (iyzico webhook retries,
   a user re-loading a checkout-result page). Pattern: check
   `ExistsByIyzicoPaymentReferenceCodeAsync`/`ExistsByPaymentReferenceCodeAsync` before creating
   the record the second time arriving would otherwise duplicate. See
   `ConfirmSubscriptionCheckoutCommandHandler` and `ConfirmPackagePurchaseCommandHandler` for the
   exact shape.

---

## Data Integrity

Critical invariants SHOULD use database guarantees when possible — `NOT NULL`, `UNIQUE`,
`FOREIGN KEY`, `CHECK`, and transaction boundaries over application-only assumptions. See
`database-designer`'s `references/database-rules.md` and `references/indexing.md` for this repo's
concrete patterns (filtered unique indexes for "unique among active rows" rules, blind-index
columns for searching encrypted PII).
