---
name: test-engineer
description: >
  Write and maintain tests for this repo's five test projects (Unit, Architecture, Integration,
  Functional, Security). Use when writing or updating any test, and whenever a change might have
  broken an existing test inventory (endpoint added/removed, handler constructor changed).
---

# Mission

Provide real confidence in behavior without creating a brittle test suite — test this repo's own
logic and contracts, not EF Core's or MediatR's behavior.

# When to Use

- Writing tests for new business logic, a new endpoint, or a new handler.
- Any endpoint added/removed/reprotected — the inventories below MUST be updated.
- A handler constructor signature changed — its existing unit tests need the new
  mock/substitute parameter.
- A bug fix that should get a regression test.

# Do Not Use

- Deciding what to test from a security-adversarial angle — that's `security-reviewer` (this
  skill implements the coverage; that skill decides what an attacker would target).

# Inputs to Inspect

- Which of the five test projects (below) actually exercises the layer you changed.
- An existing test for a similar handler/entity, to match the established naming and factory
  helper conventions before writing a new one.

# The five test projects

| Project | Tests | Tools |
|---|---|---|
| `UnitTests` | Handler + Domain behavior, mocked repositories | xUnit, NSubstitute, FluentAssertions v7 |
| `ArchitectureTests` | Layer-dependency rules (see `backend-architect`) | NetArchTest |
| `IntegrationTests` | Repositories against a real Postgres | fixture: `PostgresIdentityFixture` |
| `FunctionalTests` | HTTP endpoints, full pipeline | `WebApplicationFactory<Program>`, in-memory host, test RSA keys |
| `SecurityTests` | 401/403/400 contracts, same-company enforcement | endpoint inventory lists |

Run all five: `dotnet test RowingClub.sln`.

# Hard Rules

## MUST

- MUST test every `DomainException` branch — one test per business-rule violation.
- MUST test authorization boundaries: wrong role → 401/403; cross-company access attempt →
  `forbidden`.
- MUST update `SecurityTests/AuthEndpointSecurityTests`
  (`ProtectedEndpoints`/`PublicEndpointsWithInvalidPayload`) and
  `FunctionalTests/AuthEndpointFunctionalTests` (OpenAPI inventory) whenever an endpoint is
  added, removed, or its auth requirement changes.
- MUST update every unit test whose handler's constructor signature changed, adding the new
  substitute/mock parameter.
- MUST test this repo's own logic (algorithmic decisions like session-capacity/slot calculation,
  the package-expiry-reminder threshold walk) rather than re-testing the framework underneath it.

## MUST NOT

- MUST NOT over-mock core behavior to the point the test no longer exercises real logic.
- MUST NOT rely solely on snapshot tests for behavior that has a specific, statable expectation.

## SHOULD

- SHOULD add a regression test for a bug fix that demonstrates the previous failure mode (fails
  on the pre-fix code, passes after).
- SHOULD use a shared factory helper for common entity setup (e.g. `CompanyTestFactory.Create`)
  rather than duplicating construction boilerplate per test.
- SHOULD prefer behavior/contract tests over tests of private implementation details.

## MAY

- MAY skip an integration/functional test for a trivial, purely-declarative change with no new
  behavior — a unit test at the right layer is enough.

# Decision Framework

- Which layer actually owns the behavior being changed — Domain rule, handler orchestration,
  repository/DB behavior, or the HTTP contract itself? Test at that layer, not one above it.
- Did this change add/remove an endpoint, or change its auth requirement? If yes, the security/
  functional inventories need updating, full stop.
- Is there a `DomainException` branch with no corresponding test yet?

# Workflow

1. Identify the layer(s) the change actually touches.
2. Write/update unit tests for Domain/handler behavior, using `Substitute.For<>` for repositories.
3. Assert `DomainException`s via `.Which.ErrorCode.Should().Be("...")`.
4. If an endpoint changed, update both security and functional inventories.
5. Run `dotnet test RowingClub.sln` and confirm all five projects pass.

# Anti-Patterns

- A new endpoint with no corresponding entry in the Security/Functional inventories.
- A handler constructor change that leaves an old test silently uncompiled/skipped instead of
  updated.
- A test asserting on EF Core's or MediatR's own behavior instead of this repo's logic.
- Snake_case test names that don't follow the existing `Snake_case_behavior_sentence` convention.

# Quality Checklist

- [ ] Every `DomainException` branch touched by the change has a test.
- [ ] Authorization boundaries (wrong role, cross-company) are tested where relevant.
- [ ] Security/Functional endpoint inventories are current.
- [ ] `dotnet test RowingClub.sln` passes across all five projects.
- [ ] A regression test exists for any bug fix, where practical.

# Handoff

- What an attacker would specifically target → `security-reviewer`.
- Whether the change is actually production-safe overall → `production-readiness`.
