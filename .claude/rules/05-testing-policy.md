---
paths:
  - "src/**/*.cs"
  - "tests/**/*.cs"
---

# Backend Testing Policy

## Testing Philosophy

Test behavior, contracts, and invariants. Avoid excessive tests of internal implementation
details — test what a handler/entity guarantees, not how it happens to be written today.

---

## Unit Tests

Use for business rules, pure calculations, validation logic, and decision logic. In this repo:
`RowingClub.UnitTests` — handler + Domain behavior, repositories mocked with `Substitute.For<>`,
assertions via FluentAssertions v7.

---

## Integration Tests

Use for important boundaries: database, API handlers, queues, external service adapters. In this
repo:
- `RowingClub.IntegrationTests` — repositories against a real Postgres (`PostgresIdentityFixture`).
- `RowingClub.FunctionalTests` — HTTP endpoints via `WebApplicationFactory<Program>`, in-memory
  host, test RSA keys.
- `RowingClub.ArchitectureTests` — layer-dependency rules via NetArchTest (see
  `01-backend-principles.md` §Dependencies).
- `RowingClub.SecurityTests` — 401/403/400 contracts, same-company enforcement, endpoint
  inventories.

Run all five: `dotnet test RowingClub.sln`.

---

## Regression Tests

Bug fixes SHOULD include a regression test when practical. The test SHOULD demonstrate the
previous failure mode (fails on the old code, passes on the fix).

---

## Security Tests

Sensitive features SHOULD test unauthorized access, insufficient permission, cross-company
access, invalid input, and replay/duplicate behavior where relevant. In this repo, adding or
removing an endpoint MUST be reflected in `SecurityTests/AuthEndpointSecurityTests`
(`ProtectedEndpoints`/`PublicEndpointsWithInvalidPayload` inventories) and
`FunctionalTests/AuthEndpointFunctionalTests` (OpenAPI inventory) — a new endpoint that isn't in
these lists is an untested endpoint, not a covered one.

---

## Database Tests

Important persistence behavior SHOULD verify constraints, transactions, concurrency assumptions,
and migration compatibility where practical.

---

## What actually needs a test in this repo

- Every `DomainException` branch (one test per business-rule violation).
- Algorithmic logic (session capacity/slot calculation, package expiry threshold walk).
- Authorization boundaries: wrong role → 401/403; cross-company access → `forbidden`.
- The happy path plus at least one boundary case — test this repo's own logic, not
  EF Core's or MediatR's behavior.

A handler constructor signature change requires updating that handler's existing unit tests with
the new mock/substitute parameter — this is the most common test breakage in this codebase.
