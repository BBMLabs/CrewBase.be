# CrewBase — Runtime Issues Report

> Format per master pass §56/§57: issue → traced root cause → fix → verification.
> Only actual discovered causes are documented; nothing fabricated.

---

## ISSUE-01 — Public Site: "Kulüp adresi bulunamadı" dead-end

**Symptom:** Opening `http://localhost:5175/` shows the club-not-found alert; booking is unreachable.

**Trace:**
```text
Browser hostname = localhost            → subdomainFromHostLocal() = null
?club= query absent                     → null
resolveSubdomain()                      → null
ClubPage early-return                   → alert screen, queries never fire
```

**Root cause:** The dev URL has no subdomain and the app offered no way to supply or remember
club context. Production URLs (`kulupadi.faturebase.com`) work; the *development experience*
was broken by design omission, not by tenant-resolution logic (which mirrors backend
`TenantResolver.FromHost` correctly).

**Fix:**
1. Guided club-entry state replaces the dead-end: an explicit input for the club subdomain
   (dev) with hint that production uses `*.faturebase.com` hosts.
2. Chosen subdomain persists in `localStorage["crewbase.public.club"]`; subsequent visits
   resolve automatically.
3. Backend remains authoritative: an invalid club still returns `404 company_not_found`
   and the UI shows its error state.

**Verification:** `GET /api/v1/public/{sub}/info` returns 200 for a real club after entry;
invalid entry renders the not-found state; reload keeps the choice.

---

## ISSUE-02 — Member Portal: infinite "Yükleniyor…"

**Symptom:** First visit (or after logout) the protected route shows "Yükleniyor…" forever.

**Trace:**
```text
RequireMember guard: status === "unknown" → loading screen
SessionBootstrapper → useSessionSync hydration effect:
    current = api.session.current()          // null on first visit
    if (current !== null && !expired) apply  // never taken
→ status stays "unknown" forever             // BUG: missing else branch
```

Secondary defect at boot-with-expired-session: `isExpired(current)` was true but neither
`applySession` nor any transition ran until the 30 s interval ticked → up to 30 s stuck.

**Root cause:** The hydration effect modelled two states (valid / expired) but omitted the
third (absent). A state-machine exit path was missing — exactly the class of bug the master
pass §28 forbids ("every async state must have a defined exit path").

**Fix:** Explicit three-way branch at boot:
```text
no stored session        → markGuest()      → redirect to /giris
stored but expired       → clear() + markExpired() → /giris?oturum=sona-erdi
valid                    → applySession()
```

**Verification:** Fresh browser reaches login instantly; logged-in reload lands on home;
manually expired sessionStorage lands on expired-login variant without delay.

---

## ISSUE-03 — Member Portal booking unusable after login without club URL

**Symptom:** Even when logged in, the home screen fired malformed public requests
(`/api/v1/public//options`) whenever the URL carried no club segment.

**Root cause:** Public options/availability/consents require a `{subdomain}` route segment,
but resolution ignored the club captured at login time.

**Fix:** Subdomain resolution order extended to:
```text
host → ?club= → session-persisted club (set at login/register) → VITE_CLUB_SUBDOMAIN
```
Login/register now write the resolved subdomain into the session store. All public queries
additionally gate on `enabled: subdomain !== ""`, with an explicit fallback card if context
is somehow absent.

**Verification:** Login via `?club=demo` once → later visits to plain `/` resolve booking,
availability and consents through the stored club; member mutations remain claim-scoped
(no client tenant IDs added anywhere).

---

## ISSUE-04 — Panel sidebar ThemeToggle invisible in light theme

**Root cause:** Toggle placed on the always-dark rail used auto tone (dark ink icon on dark
background in light mode).

**Fix:** `ThemeToggle` gained `tone="onDark"`; panel sidebar passes it.

**Verification:** Visible and clickable in both themes.
