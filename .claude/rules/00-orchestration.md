# Backend Skill Orchestration

## Core Rule

Use the minimum number of skills required for the task. Do not invoke every backend skill
automatically — a one-line bug fix does not need `feature-planner` through
`production-readiness`. See `CLAUDE.md` for the full priority order this orchestration serves.

---

## New Feature

For substantial backend features:

```
feature-planner
  → backend-architect
  → api-designer          (if API changes)
  → database-designer      (if persistence changes)
  → auth-security           (if identity or permissions are involved)
  → backend-reliability      (if external systems or async work exist)
  → implementation
  → test-engineer
  → relevant reviewers
  → production-readiness
```

---

## API Change

```
api-designer
  → auth-security       (when protected)
  → test-engineer
  → security-reviewer   (when externally exposed)
```

---

## Database Change

```
database-designer
  → migration-manager
  → backend-performance   (when query behavior changes)
  → test-engineer
```

---

## Authentication / Authorization

```
auth-security
  → backend-architect   (if architecture changes)
  → security-reviewer
  → test-engineer
```

---

## Production Incident

```
debugger
  → backend-observability
  → relevant domain skill
  → test-engineer         (for regression coverage)
  → code-reviewer
```

---

## Performance Problem

```
backend-performance
  → backend-observability
  → database-designer    (if database related)
  → backend-architect     (if systemic)
```

Do not redesign architecture before identifying the bottleneck — observe and measure first (see
`04-reliability-observability.md`).

---

## External Integration

```
api-designer
  → backend-reliability
  → auth-security          (when credentials/signatures exist)
  → backend-observability
  → test-engineer
```

This repo's own iyzico integrations are the reference example — see `04-reliability-observability.md`
§Worked example.

---

## Release

For substantial production releases consider:

```
security-reviewer
  → migration-manager
  → production-readiness
```

---

## Conflict Resolution

When rules or skills conflict, this priority wins (highest first) — full reasoning and worked
examples are in `CLAUDE.md`:

```
Security
> Data integrity
> Correctness
> Authorization
> Reliability
> Backward compatibility
> Observability
> Performance
> Architecture
> Developer ergonomics
> Elegance
```

## Activation matrix

| Task | Skills |
|---|---|
| Simple endpoint | `api-designer` + `test-engineer` |
| CRUD feature | `backend-architect` + `api-designer` + `database-designer` + `test-engineer` |
| Large feature | `feature-planner` + `backend-architect` + relevant skills |
| Login | `auth-security` + `api-designer` + `test-engineer` + `security-reviewer` |
| Permissions | `auth-security` + `security-reviewer` + `test-engineer` |
| DB schema change | `database-designer` + `migration-manager` |
| Slow endpoint | `backend-performance` |
| Production latency | `debugger` + `backend-performance` + `backend-observability` |
| Queue/background worker | `backend-reliability` + `backend-observability` + `test-engineer` |
| Webhook | `api-designer` + `backend-reliability` + `auth-security`/`security-reviewer` |
| Third-party API integration | `api-designer` + `backend-reliability` + `backend-observability` |
| Production incident | `debugger` + `backend-observability` |
| Release | `production-readiness` |
| General PR review | `code-reviewer` |
| Data migration | `migration-manager` + `database-designer` |
| Security audit | `security-reviewer` |

If a task doesn't map cleanly to a row above, pick the smallest combination that covers what it
actually touches — don't default to the full pipeline "to be safe."
