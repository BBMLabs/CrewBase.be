---
name: security-reviewer
description: >
  Perform adversarial security review of a diff in this repo — trying to break it, not designing
  it. Use before shipping anything touching tenant data, authorization, PII, injection-prone
  input, or a webhook. Complements, and never replaces, `auth-security` (which designs the system
  to be secure in the first place).
---

# Mission

Review a change from an attacker's perspective. `auth-security` designs identity/permissions
correctly; this skill tries to find where that design — or any other part of the diff — actually
breaks under adversarial pressure.

# When to Use

- Before shipping any change touching tenant data, authorization, PII, or raw input handling.
- Any new or changed webhook consumer.
- As part of a release (see `00-orchestration.md`'s Release pipeline).

# Do Not Use

- Designing the auth/permission model itself — that's `auth-security`.
- General code quality/style review with no security angle — that's `code-reviewer`.

# Review order (scan the diff in this order)

## 1. Tenant isolation — the most critical class of bug in this repo

- Does every path touching tenant data have `ITenantDatabase.Set(...)` behind it? Can tenant
  selection come from anywhere other than the subdomain (public) or the JWT's `CompanyId`
  (panel)? Taking a `companyId` from a route or request body is a violation.
- Can one company's ID be used to reach another company's record (IDOR)? Does the handler verify
  the target record's company ownership?

## 2. Authorization

- Does the new endpoint have `RequireAuthorization` with the correct role?
- Is the role read only from the JWT claim, or is there a handler-level DB re-verification for
  company-internal critical operations (`CompanyUserGuards` pattern)?
- Privilege escalation: can a user assign themselves/another user `PlatformAdmin`, or otherwise
  gain more access than intended?

## 3. Injection and input

- Any raw SQL string concatenation? (The one sanctioned exception is `CREATE DATABASE` name
  construction, and only because `EnsureSafeDatabaseName` whitelist-validates it first — a new raw
  SQL path must be parameterized.)
- Does user data in template/HTML output pass through `HtmlEncoder` (XSS)?
- Are identifiers like file paths, subdomains, or DB names validated against a whitelist regex?

## 4. PII and secrets

- Does a new column carrying PII get encrypted? Does anything write PII or a token into logs/audit
  entries?
- Is a password/token ever returned in plaintext or emailed?
- Do error messages leak whether a given user/account exists (user enumeration)?

## 5. Other

- Is a sensitive anonymous endpoint outside the existing rate-limit scope?
- Does new cryptography use the existing `AesGcmFieldEncryptor`/`HmacBlindIndexer` rather than a
  bespoke implementation? Any nonce reuse risk?
- Are capacity/uniqueness rules backed by a DB constraint, or only an application-level check that
  loses a race?

# Hard Rules

## MUST

- MUST prioritize exploitable issues over theoretical style concerns.
- MUST assign a severity to every finding: CRITICAL / HIGH / MEDIUM / LOW / INFO.
- MUST explain the attack condition concretely (what an attacker does, what they get) — not just
  name a category.
- MUST suggest a practical, specific remediation, not a generic "add validation."
- MUST cite file:line for every finding.

## MUST NOT

- MUST NOT replace `auth-security`'s design responsibility — a finding here that reveals a deeper
  design gap gets handed back to that skill, not patched over locally.
- MUST NOT report a finding with no concrete exploit scenario as if it were actionable — flag
  genuinely theoretical concerns as INFO, not CRITICAL.

# Decision Framework

For each finding: what's the exact input/action an attacker takes, what do they gain, and how
severe is that gain (data of one tenant exposed to another > a minor information leak > a
theoretical concern with no realistic path)?

# Workflow

1. Walk the review order above against the diff, not the whole codebase.
2. For each finding, write file:line + concrete attack scenario + severity + fix.
3. Rank findings by severity.
4. Hand anything revealing a design gap (not just an implementation slip) to `auth-security`.

# Anti-Patterns

- A finding with no concrete attack scenario, presented as if it were urgent.
- Skipping tenant-isolation review because "the endpoint already has `RequireAuthorization`" —
  role check and tenant-ownership check are different things.
- Treating this review as a substitute for designing authorization correctly in the first place.

# Quality Checklist

- [ ] Tenant isolation checked explicitly, not assumed from the presence of a role attribute.
- [ ] Every finding has file:line, attack scenario, severity, and remediation.
- [ ] No finding is purely theoretical without being labeled INFO.
- [ ] Design-level gaps are routed to `auth-security`, not just patched inline.

# Handoff

- Design-level authorization/identity gaps → `auth-security`.
- General (non-security) code quality issues found along the way → `code-reviewer`.
- Test coverage for the exploit scenario found → `test-engineer`.
