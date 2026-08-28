---
name: production-readiness
description: >
  Final production readiness/release gate for this repo — configuration, deployment ordering,
  migrations, rollback, and go-live checks. Use before a production deploy or any
  configuration/docker-compose change. This is a release gate, not an architecture skill — it
  does not redesign the feature.
---

# Mission

Determine whether a change is actually safe to run in production — not whether it's well
designed (that's earlier skills' job by this point).

# When to Use

- Before a production deploy.
- Any change to environment variables, `docker-compose.yml`, or startup ordering.
- As the final step of a release pipeline (see `00-orchestration.md`).

# Do Not Use

- MUST NOT redesign the feature at this stage — if a genuine design problem is found here, that's
  a signal an earlier skill (`backend-architect`/`database-designer`/`auth-security`) needs to be
  revisited, not something to patch in the release review itself.

# Inputs to Inspect

- `.env.example` vs. `.env.production` vs. `docker-compose.yml` — a new env var must exist in all
  three consistently (see the historical incident below).
- The startup sequence in `Program.cs`/`Bootstrapper` if migration/hosted-service ordering changed.

# Configuration inventory

All settings come from env (`DotEnvFileLoader` → `.env.developer`/`.env.production`, selected by
`APP_ENV`). Critical groups:

- `POSTGRES_*` — catalog DB; tenant DBs live on the same server, provisioned automatically.
- `REDIS_*` — idempotency store; app stays up if Redis is down (`AbortOnConnectFail=false`).
- `JWT_*` / RSA keys, `ENCRYPTION_*` (field encryption + blind-index keys) — **losing a key means
  encrypted data can never be recovered.** Back these up; rotate via `KeyVersion`, never a silent
  swap.
- `SMTP_*` — welcome + reminder emails. A misconfiguration doesn't block registration (the error
  is caught and audit-logged), it just means no email goes out.
- `AUTH_RATE_LIMIT` — per-IP per-minute limit on auth endpoints.

# Startup order (do not change without a reason)

1. Catalog migration (`EfMigrationHostedService`).
2. Tenant migration sweep (`TenantMigrationHostedService` — per company, isolated failures).
3. `ReminderWorker`'s per-minute tour begins.

# Hard Rules

## MUST

- MUST verify `dotnet test` is green and `dotnet publish -c Release` succeeds.
- MUST verify any newly-added env var exists in `.env.production` AND `docker-compose.yml` — a
  var present only in `.env.production` but missing from compose has taken production down
  before in this project; treat this as a known, recurring failure mode, not a hypothetical.
- MUST verify migrations are idempotent, and that a NOT NULL/unique constraint added to a
  populated table has a backfill (see `migration-manager`).
- MUST verify the Postgres user has `CREATEDB` (required for tenant provisioning).
- MUST verify `/health` returns 200 and that Scalar/OpenAPI is Development-only.
- MUST verify no PII/secret is leaking into logs, and that the Prometheus scrape target is
  correctly configured.
- MUST verify DB backups cover tenant databases, not only the catalog DB.

## MUST NOT

- MUST NOT redesign the feature at this stage (see Do Not Use above) — classify a discovered
  design problem as a blocker for an earlier skill, don't silently absorb it here.
- MUST NOT treat a passing test suite alone as sufficient — configuration/deployment issues are
  exactly the class of problem tests don't catch.

## SHOULD

- SHOULD double check, on any real domain cutover, that `TenantResolver.BaseDomain` is the only
  place that needs to change, and that the reverse proxy forwards the `Host` header (subdomain
  tenant resolution depends on it) and wildcard DNS/TLS for `*.faturebase.com` is in place.

# Decision Framework

Classify every check as READY, READY WITH WARNINGS (non-blocking but should be tracked), or NOT
READY (blocks the release) — never leave a check unresolved without one of these three labels.

# Workflow

1. Run the go-live checklist below in full.
2. Classify each item as passed, warning, or blocker.
3. List blockers separately from recommendations — a blocker must be resolved before release, a
   recommendation may ship with a follow-up noted.
4. Output an overall READY / READY WITH WARNINGS / NOT READY verdict.

# Go-live checklist

- [ ] `dotnet test` green; `dotnet publish -c Release` succeeds.
- [ ] `.env.production` current; every new env key is also in `docker-compose.yml`.
- [ ] Migrations idempotent; NOT NULL/unique additions to populated tables have backfill.
- [ ] Postgres user has `CREATEDB`.
- [ ] `/health` returns 200; Scalar/OpenAPI is dev-only.
- [ ] No PII/secret leakage in logs; Prometheus scrape target configured.
- [ ] DB backups cover tenant databases, not just catalog.
- [ ] Security review completed for anything touching tenant data/auth (`security-reviewer`).
- [ ] Rollback path is explicit for this specific change.

# Anti-Patterns

- Shipping a new env var to `.env.production` without adding it to `docker-compose.yml`.
- Treating "tests pass" as equivalent to "safe to deploy."
- Silently absorbing a design problem found at release time instead of escalating it.
- A destructive migration bundled with the release with no rollback story.

# Quality Checklist

(Same as the go-live checklist above — this skill's output IS that checklist, classified.)

# Handoff

- A design problem discovered here → back to `backend-architect`/`database-designer`/`auth-security`.
- A migration safety concern → `migration-manager`.
- A security concern → `security-reviewer`.
