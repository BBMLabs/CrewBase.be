---
name: code-reviewer
description: >
  General code review for this repo — correctness first, then security, then simplicity/
  consistency, using this repo's actual style and pattern contracts. Use for any non-trivial diff
  review. Not a substitute for `security-reviewer`'s adversarial pass on security-sensitive code.
---

# Mission

Find meaningful implementation problems without generating noise — correctness first, security
second (deferring depth to `security-reviewer`), simplicity/consistency third.

# When to Use

- Any non-trivial diff review.
- Before merging a feature branch.

# Do Not Use

- In place of `security-reviewer` for anything genuinely security-sensitive — this skill checks
  for security *smells* in passing; the adversarial deep-dive is a separate pass.

# Review priority

1. Correctness
2. Data integrity
3. Security
4. Bugs
5. Reliability
6. Performance
7. Maintainability
8. Style

# Correctness checks specific to this repo

- Did a business rule leak into a handler instead of living on the entity? (It belongs on the
  entity.)
- Is the request the right MediatR shape — `ICommand<T>` for writes, `IRequest<T>` for reads? A
  tenant-writing handler that forgets to call
  `ISchedulingUnitOfWork.SaveChangesAsync` silently discards its own change — this is the single
  most common correctness bug in this codebase.
- Are null/empty cases handled the established way — `?? CompanySettings.Default()`, an explicit
  null check after `FirstOrDefault`?
- Is `DateOnly`/`TimeOnly`/`DateTimeOffset` used consistently, with local-time math going through
  `settings.NowLocal()` rather than raw `DateTime.Now`?

# Consistency contracts specific to this repo

- Errors: `DomainException("snake_code", "Turkish user message")`; endpoint early returns via
  `ApiResponse.Fail`. Check `references/error-format.md`'s dictionary before adding a new code.
- Naming: `*CommandHandler`, `*QueryHandler`, `*Repository`, `*Dto`, `*AtUtc`.
- Comments: Turkish, only for a constraint/intent that isn't readable from the code itself — never
  a comment that just restates what the code does.
- XML doc only at the class level, and only when it adds real architectural context.
- Domain entities never cross the public API surface directly — map to a DTO.

# Hard Rules

## MUST

- MUST prioritize actionable issues — every finding should have a concrete fix, not just a
  complaint.
- MUST distinguish blocking issues from optional improvements explicitly.
- MUST check that repeated resolve/validation logic (three-plus occurrences) has been extracted
  to a helper, matching the existing `ResolveOwnCompanyAsync`-style pattern in endpoint files.
- MUST check for unused `using`/parameters/records left behind.

## MUST NOT

- MUST NOT invent a hypothetical problem with no evidence in the actual diff.
- MUST NOT request an abstraction purely for stylistic reasons — an interface with no second
  consumer is suspect, not automatically wrong (repository interfaces are the one exception,
  required by this repo's layering rules — see `backend-architect`).

## SHOULD

- SHOULD flag premature abstraction the same way `backend-architect`/`01-backend-principles.md`
  would — this skill is a second line of defense for that principle, not a separate standard.

# Decision Framework

- Is this finding backed by a specific line in the diff, or is it a general worry?
- Does it block merge (correctness/data-integrity/security), or is it an optional improvement
  (style/maintainability)?
- Is there a simpler way to express the same behavior using an existing pattern in this codebase?

# Workflow

1. Read the diff for correctness first: entity vs. handler rule placement, `ICommand`/`IRequest`
   choice, `SaveChangesAsync` presence for tenant writes.
2. Check consistency contracts (errors, naming, comments, DTO boundary).
3. Check for premature abstraction or duplicated logic that should be a helper.
4. Rank findings: blocking vs. optional, each with file:line and a concrete fix.
5. Confirm the close-out bar: `dotnet build` clean, `dotnet test` green, endpoint test
   inventories updated if the endpoint surface changed.

# Anti-Patterns

- A finding with no file:line or no concrete suggested fix.
- Nitpicking style on a diff that has an actual correctness bug unaddressed.
- Requesting a new abstraction for a pattern used exactly once.
- A tenant-writing handler with no `SaveChangesAsync` call, missed in review.

# Quality Checklist

- [ ] Correctness issues (rule placement, command/query shape, `SaveChangesAsync`) checked first.
- [ ] Error/naming/comment conventions match the existing repo style.
- [ ] No unused using/param/record left behind.
- [ ] Findings are ranked blocking vs. optional, each with file:line.
- [ ] `dotnet build` clean + `dotnet test` green + endpoint inventories current (if applicable).

# Handoff

- Deep security review of a sensitive change → `security-reviewer`.
- A correctness issue that's actually a design gap → `backend-architect`/`database-designer`.
- Missing test coverage found during review → `test-engineer`.
