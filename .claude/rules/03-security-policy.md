---
paths:
  - "src/**/*.cs"
---

# Backend Security Policy

## Trust Boundaries

Treat all external data as hostile until validated. Never derive authorization from
user-controlled values — most critically in this repo: **never take `companyId`/tenant identity
from the request body or route**. Public endpoints resolve tenant from the subdomain
(`TenantResolver.ResolveBySubdomainAsync`); panel endpoints resolve it from the caller's JWT
(`ICurrentUser.CompanyId` → `TenantResolver.ResolveByCompanyIdAsync`). A route/body-supplied
`companyId` is a tenant-isolation violation — see `security-reviewer`'s §1.

---

## Authentication

Credentials and session material MUST be protected.

- JWT: RSA-signed (`RsaJwtTokenService`); claims carry `UserId`, `Email`, `Role`, `CompanyId`.
  Refresh tokens are opaque, SHA-256 hashed, with family tracking and reuse detection.
- Password storage MUST use Argon2id (`Argon2IdPasswordHasher`) — never store or email a plaintext
  password (the welcome email intentionally does not contain one).

---

## Authorization

Authentication does not imply authorization. Every protected resource operation MUST verify
access, and in this repo that check has two layers, both required:

1. `RequireAuthorization(Roles = ...)` at the endpoint — necessary but not sufficient.
2. The handler re-verifies the caller server-side: reload the caller from the DB, confirm their
   role and `CompanyId` match the target resource (`CompanyUserGuards.EnsureCallerIsCompanyAdminAsync`
   is the reference pattern). A role claim in the JWT is a hint, not proof.

Object-level authorization MUST be enforced server-side: a company admin operating on another
company's record must get `forbidden`, not a successful response. `PlatformAdmin` can never be
self-assigned or assigned by a `CompanyAdmin`; a user cannot demote their own admin access
(`cannot_demote_self`).

---

## Injection

Never construct unsafe database or shell commands using raw untrusted input. Use parameterized
database operations. The one accepted exception in this repo is tenant `CREATE DATABASE` name
construction, which is safe only because `EnsureSafeDatabaseName` whitelist-validates the name
first (`[a-z0-9_]`, length-bounded) — do not add a second unvalidated raw-SQL path.

---

## Secrets

Never log:
- passwords
- access tokens
- refresh tokens
- API secrets (iyzico, SMTP, reCAPTCHA)
- session identifiers
- private keys / field-encryption keys

Field-encryption and JWT signing keys are especially sensitive here: **losing the field-encryption
key makes encrypted PII permanently unrecoverable.** Rotation goes through `KeyVersion`, never a
silent key swap.

---

## External Requests

User-controlled destinations MUST be reviewed for SSRF risk before the backend makes an outbound
call to them.

---

## File Handling

Uploads MUST be validated for allowed size, expected type, storage path, and authorization. This
repo's package-image upload endpoint is the reference: content-type whitelist
(`image/jpeg`/`image/png`/`image/webp`), 2 MB cap, server-generated filename (never the client's
original filename), path scoped under `/uploads/packages/{packageId}/`. Do not trust a file
extension alone.

---

## Errors

Production errors MUST NOT reveal stack traces, database credentials, internal paths, or secret
values — see `02-api-data-contracts.md` §Error Contracts for the two sanctioned error shapes.
