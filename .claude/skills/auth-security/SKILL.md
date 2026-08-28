---
name: auth-security
description: >
  Design and review authentication, authorization, session management, roles, and field
  encryption for this repo. Use for any change touching login, JWT/refresh tokens, roles,
  company-boundary enforcement, or PII encryption. Authentication answers "who are you";
  authorization answers "are you allowed to do this" — never confuse them.
---

# Mission

Ensure identity and permissions in this repo cannot be bypassed — every protected operation is
actually re-checked server-side, and no client-supplied value is ever trusted for who a user is
or what company they belong to.

# When to Use

- Login/logout/session/refresh-token work.
- Any new role or role-gated capability.
- Any endpoint touching another user's or another company's data.
- Adding/changing an encrypted PII field.

# Do Not Use

- Adversarial/attacker-oriented review of an already-designed feature — that's `security-reviewer`
  (this skill designs the system to be secure; that one tries to break it).
- Pure architecture with no identity/permission dimension — that's `backend-architect`.

# Inputs to Inspect

- Existing `RequireAuthorization(Roles = ...)` usage on sibling endpoints on the same surface.
- `CompanyUserGuards.EnsureCallerIsCompanyAdminAsync` — the reference pattern for handler-level
  re-verification.
- The `TenantDbContext` PII encryption converters, if the change touches a PII field.

# Identity architecture

- JWT: RSA-signed (`RsaJwtTokenService`); claims: `UserId`, `Email`, `Role`, `CompanyId`. Refresh
  tokens are opaque, SHA-256 hashed, with token-family tracking and reuse detection.
- Passwords: Argon2id (`Argon2IdPasswordHasher`) — plaintext is never logged and never emailed
  (the welcome email deliberately contains no password).
- Roles: `PlatformAdmin`, `CompanyAdmin`, `Employee`. 2FA endpoints exist but are deliberately
  inactive right now.

# Hard Rules

## MUST

- MUST enforce authorization server-side, in two layers: `RequireAuthorization(Roles=...)` at the
  endpoint (necessary, not sufficient) AND a handler-level re-verification that reloads the
  caller from the DB and checks their real role and `CompanyId` match the target
  (`CompanyUserGuards.EnsureCallerIsCompanyAdminAsync` pattern).
- MUST verify object-level ownership for every operation on a specific record — a target record
  outside the caller's own company returns `forbidden`, never succeeds.
- MUST route all tenant-data access through `TenantResolver.ResolveByCompanyIdAsync` →
  `ITenantDatabase` derived from the caller's own JWT `CompanyId` — never from a client-supplied
  company/tenant id.
- MUST consider token revocation and expiry for anything issuing a new kind of credential.
- MUST encrypt new PII fields using the existing `TenantDbContext` converter pattern (AES-256-GCM);
  add a blind-index column if it needs to be searched.

## MUST NOT

- MUST NOT trust a role/company id supplied by the client in any form (JWT claim is read, but the
  claim itself is not sufficient authorization — see MUST above).
- MUST NOT store or transmit a plaintext password, including by email.
- MUST NOT let a `CompanyAdmin` assign `PlatformAdmin`, and MUST NOT let any user demote their
  own admin access (`cannot_demote_self`).
- MUST NOT write encryption/JWT signing keys into code or logs — they come from env
  (`EncryptionOptions`, JWT key config) only.

## SHOULD

- SHOULD keep sensitive-action audit logging (`IAuditLogger`) consistent with the existing event
  naming (`LOGIN_*`, `COMPANY_USER_*`, `COMPANY_SUBSCRIPTION_*`) rather than inventing a parallel
  logging path.
- SHOULD keep auth endpoints under the existing per-IP rate limit (`AUTH_RATE_LIMIT`,
  `RateLimitingSetup.AuthPolicy`).

## MAY

- MAY recommend a frontend storage practice (token in memory + `sessionStorage`, never
  `localStorage`) — but the actual enforcement is always this skill's server-side rules, not a
  frontend convention (see `frontend-security` in the CrewBase.fe repo for that side).

# Decision Framework

- Is this authentication (who) or authorization (allowed to do what) — don't conflate the two in
  the design.
- Does this operation target a specific record? If yes, what verifies the caller's company owns
  it?
- Does this introduce a new credential/token type? What's its expiry and revocation story?
- Does this touch a new PII field? Encrypted, and searchable via blind index if needed?

# Workflow

1. Classify the change: authentication surface, authorization surface, or both.
2. Design the endpoint-level role gate.
3. Design the handler-level server-side re-verification (company/ownership match).
4. If PII is involved, apply the encryption/blind-index pattern.
5. Hand off to `security-reviewer` for an adversarial pass and to `test-engineer` for
   authorization-boundary test coverage.

# Anti-Patterns

- Authorization decided only by a JWT role claim, with no handler-level re-check.
- A `companyId` accepted from the request body/route and trusted for scoping a query.
- A password or token returned in a response body, logged, or emailed.
- A new PII column left unencrypted "temporarily."

# Quality Checklist

- [ ] Endpoint-level role gate AND handler-level re-verification both present.
- [ ] Cross-company access returns `forbidden`, verified against a real "other company" case.
- [ ] No client-supplied company/tenant identity is trusted anywhere in the path.
- [ ] New PII is encrypted, with a blind index if searchable.
- [ ] Sensitive actions are audit-logged consistent with existing event names.

# Handoff

- Adversarial review of the finished design → `security-reviewer`.
- Authorization-boundary test coverage → `test-engineer`.
- Architectural placement of a new identity concern → `backend-architect`.
