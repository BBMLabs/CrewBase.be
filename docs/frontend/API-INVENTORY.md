# CrewBase — Frontend API Inventory

> Source of truth: `src/RowingClub.Api/Endpoints/*.cs` (verified line-by-line, 2026-08).
> Documentation cross-checked against implementation; **implementation wins** on any conflict.
> Status legend: `VERIFIED` = contract confirmed in backend source. `IMPLEMENTED` = frontend integration shipped. `NOT_VERIFIED` = not yet checked. `BLOCKED` = cannot integrate.

## ⚠ Backend build status (2026-08-22)

The committed backend does **not compile**: `RowingClub.Scheduling.Domain` has no `Logs`
namespace (no `Logs/` folder), while ~10 Application files import
`RowingClub.Scheduling.Domain.Logs` (`IMemberLogRepository`, `MemberLog.Record`). Working tree
is clean ⇒ the folder was never committed. Consequences:

- Live end-to-end verification of ALL endpoints is **BLOCKED** until the backend owner restores
  the missing domain layer. Frontend integration proceeds against the source-verified contract
  (DTO shapes were read from handler code, which is intact).
- Log-related endpoints (7.13, 7.14, member activity views) additionally depend on the missing
  repository at runtime — flagged `BACKEND_INCOMPLETE` in coverage audit.
- Per zero-hallucination policy the frontend did NOT patch or stub the backend.

## Global Conventions (verified)

- **Base URL (dev):** `http://localhost:5283`; API prefix `/api/v1`.
- **Success envelope** (`src/RowingClub.Api/ApiResponse.cs`):
  `{ "success": true, "data": <T|null>, "message": string|null, "code": string|null }`
- **Error envelope** (endpoint early-returns): same shape with `"success": false`, non-null `code`.
- **ProblemDetails** (domain/validation errors via `GlobalExceptionHandler`): RFC7807 JSON
  `{ type, title, status, detail, instance, traceId, code, errors? }` where `errors` is a
  field→string[] map (FluentValidation only). Content-Type `application/problem+json`.
- **Auth:** `Authorization: Bearer <JWT>` (RS256). Claims: `sub`, `email`, `jti`, `iat`,
  `role` (`ClaimTypes.Role`, values `PlatformAdmin|CompanyAdmin|Employee|Member`),
  `company_id` (company users + members).
- **Rate limiting:** policy `auth` on `/api/v1/auth/*` and `/api/v1/public/{subdomain}/members/*`;
  10 req/min/IP default → `429`.
- **Tenant:** server-resolved only. Public/member-auth: `{subdomain}` route segment.
  Panel + member API: JWT `company_id` claim. No client-supplied tenant is ever trusted.
- **Dates:** `yyyy-MM-dd` strings; **times:** `HH:mm`; **timestamps:** ISO-8601 UTC
  (`DateTimeOffset`). **Enums:** serialized as strings.
- **Pagination:** none anywhere. `take` query params clamp server-side (logs ≤500 default 100,
  messages ≤200 default 100, feed fixed 50).
- **CORS:** `localhost`, `*.localhost`, `*.faturebase.com` origins allowed,
  all headers/methods, credentials allowed.

---

## 1. Auth — `/api/v1/auth` (rate-limited)

Source: `AuthEndpoints.cs`. All responses use envelope. Validation via FluentValidation
(400 ProblemDetails `validation_error` + `errors` map).

| # | Method | Route | Auth | Request | Response (`data`) | Errors | UI | Status |
|---|--------|-------|------|---------|-------------------|--------|----|--------|
| 1.1 | POST | `/companies/register` | Anon | `{ companyName, adminEmail, adminPassword, phone?, contactEmail?, address? }` | `RegisterCompanyResponse { companyId, adminUserId, companyName, adminEmail, subdomain, siteUrl, createdAtUtc }` (201) | 409 `company_name_taken`, `email_already_registered`; validation: name ≤200 req; email pattern ≤254 req; password ≥10 ≤128 + [A-Z][a-z][0-9][special] req; phone regex `^\+?[0-9\s\-()]{7,20}$` opt; contactEmail pattern opt; address ≤500 | Panel: signup | VERIFIED |
| 1.2 | POST | `/login` | Anon | `{ email, password }` | `LoginResult { requiresTwoFactor: false, pendingTwoFactorToken: null, accessToken, accessTokenExpiresAtUtc, refreshToken }` | 401 `unauthorized` (5 fails → 15 min lockout) | Panel login | VERIFIED |
| 1.3 | POST | `/refresh` | Anon+token | `{ refreshToken }` | `RefreshTokenResponse { accessToken, accessTokenExpiresAtUtc, refreshToken }` | 401 `unauthorized`; reuse → whole family revoked | Panel silent refresh | VERIFIED |
| 1.4 | POST | `/logout` | Anon+token | `{ refreshToken }` | message "Oturum kapatıldı." (idempotent) | validation | Panel logout | VERIFIED |
| 1.5 | POST | `/logout-all` | Bearer | — | message "Tüm oturumlar kapatıldı." | 401 | Panel security action | VERIFIED |
| 1.6 | POST | `/forgot-password` | Anon | `{ email }` | generic success message | validation | Panel forgot pw | VERIFIED |
| 1.7 | POST | `/reset-password` | Anon | `{ email, token, newPassword }` | success message | 409 (invalid/expired token); validation pw rules as 1.1 | Panel reset pw | VERIFIED |
| 1.8 | POST | `/verify-email` | Anon | `{ email, token }` — token = 6-digit OTP | success message | 409; validation | Panel verify email | VERIFIED |
| 1.9 | POST | `/send-verification-email` | Anon | `{ email }` | success message | validation | Panel resend OTP | VERIFIED |

2FA routes (`/login/verify-2fa`, `/2fa/setup|enable|disable|recovery-codes`) are **deliberately
not mapped** → 404. Handlers exist but no HTTP surface. **No UI. Do not build.**

## 2. Platform Admin — `/api/v1/platform` (role: `PlatformAdmin`)

Source: `AdminEndpoints.cs`.

| # | Method | Route | Request | Response | Errors | UI | Status |
|---|--------|-------|---------|----------|--------|----|--------|
| 2.1 | GET | `/companies` | — | `PlatformCompanyDto[] { id, name, subdomain, status: "PendingApproval"\|"Active"\|"Suspended", phone?, contactEmail?, createdAtUtc }` | 401/403 | Future platform-admin app | VERIFIED (NO_UI this pass) |
| 2.2 | GET | `/stats` | — | `PlatformStatsDto { totalCompanies, activeCompanies, suspendedCompanies, registeredThisMonth }` | 401/403 | Future app | VERIFIED (NO_UI this pass) |
| 2.3 | GET | `/companies/pending` | — | `PendingCompanyDto[] { companyId, name, contactEmail?, phone?, address?, createdAtUtc }` (practically empty) | 401/403 | NO_UI | VERIFIED (NO_UI) |
| 2.4 | POST | `/companies/{companyId}/approve` | — | message | 401/403 | Future app | VERIFIED (NO_UI this pass) |
| 2.5 | POST | `/companies/{companyId}/suspend` | — | message | 401/403 | Future app | VERIFIED (NO_UI this pass) |

## 3. Admin placeholders — `/api/v1/admin`

| # | Method | Route | Auth | Response | UI | Status |
|---|--------|-------|------|----------|----|--------|
| 3.1 | GET | `/admin/{companyId}/dashboard` | CompanyAdmin + SameCompany | placeholder text | NO_UI (placeholder) | VERIFIED (NOT_USER_FACING) |
| 3.2 | GET | `/admin/{companyId}/users` | CompanyAdmin + SameCompany | placeholder text | NO_UI | VERIFIED (NOT_USER_FACING) |
| 3.3 | GET | `/admin/profile` | any authed | placeholder text | NO_UI | VERIFIED (NOT_USER_FACING) |
| 3.4 | GET | `/admin/employee/tasks` | Employee | placeholder text | NO_UI | VERIFIED (NOT_USER_FACING) |

Employee role has **no real panel endpoints** — documented backend gap, no invented UI.

## 4. Public Site — `/api/v1/public/{subdomain}` (anonymous)

Source: `PublicSiteEndpoints.cs`.

| # | Method | Route | Query/Body | Response | Errors | UI | Status |
|---|--------|-------|-----------|----------|--------|----|--------|
| 4.1 | GET | `/site/{subdomain}` | — | server-rendered HTML | plain 404 text | NO_UI (backend renders it) | VERIFIED (NO_UI) |
| 4.2 | GET | `/` (root w/ subdomain host) | — | HTML or greeting | — | NO_UI | VERIFIED (NO_UI) |
| 4.3 | GET | `/info` | — | `{ name, subdomain, siteUrl, phone?, contactEmail?, address? }` (anonymous object) | env 404 `company_not_found` | Public: club header/contact | VERIFIED |
| 4.4 | GET | `/options` | — | `PublicOptionsDto { openingTime, closingTime, slotMinutes, openDays: number[], minNoticeHours, maxAdvanceDays, reminderOptions: number[], defaultReminderMinutes, boatClasses: {value,label,capacity}[], packages: {id,name,description?,sessionCount,price}[], levelLabels: string[11] }` | env 404 `company_not_found` | Public booking form config | VERIFIED |
| 4.5 | GET | `/consents` | — | `ConsentStateDto[] { key, title, body, scope: "Booking"\|"Member", required, icon, accepted:false, acceptedAtUtc:null, ipAddress:null }` | env 404 `company_not_found` | Guest consent display | VERIFIED |
| 4.6 | GET | `/availability` | `date?` (default today), `boatClass?` (default "1x"), `phone?` | `SlotDto[] { time: "HH:mm", available, seatsLeft }` | env `invalid_date`, 404 `company_not_found` | Public/member slot picker | VERIFIED |
| 4.7 | POST | `/appointments` | `PublicBookingRequest { fullName, phone, email?, date, time, boatClass?, experienceAcknowledged, teammateName?, note?, reminderMinutes?, acceptedConsents?: string[] }` | `BookAppointmentResponse { appointmentId, sessionId, date, startTime, boatClass, level, boatName?, instructorName?, reminderMinutes?, status }` (201) | env: `invalid_date`, `invalid_time`; domain: `guest_class_restricted`, `no_2x_partner_available`, `consents_required`, `already_booked`, `closed_date`, `too_soon`, `too_far`, `invalid_slot`, `slot_full`, `boat_class_unavailable`, `invalid_reminder`; validation as §5 row 5.3 minus member-only fields | Public guest booking | VERIFIED |

Guest rules verified: identity matched by phone→email (existing member level applies);
non-members selecting `1x`/`2x` must send `experienceAcknowledged: true` (else
`guest_class_restricted`); for `2x`, a `teammateName` is first checked against existing sessions
in that slot for a **mutual** name match (the other appointment's own `teammateName` must equal
this request's `fullName`) — if found, both land in the same session regardless of level/account;
otherwise non-member `2x` bookings never open a fresh solo session — they join the lowest-level
joinable existing `2x` session for that slot, or get `no_2x_partner_available`; Booking consents
re-required every request; package deduction hard-disabled (`UsePackage:false`); one appointment
per person per slot (`already_booked`).

## 5. Member Auth — `/api/v1/public/{subdomain}/members` (rate-limited)

Source: `MemberEndpoints.cs`. Success = envelope with anonymous object.

| # | Method | Route | Body | Response (`data`) | Errors | UI | Status |
|---|--------|-------|------|-------------------|--------|----|--------|
| 5.1 | POST | `/register` | `MemberRegisterRequest { fullName, phone, email, password, acceptedConsents?: string[] }` | `{ member: MemberDto, accessToken, expiresAtUtc }` (message "Üyeliğiniz oluşturuldu.") | 409 `member_exists`, `email_taken`; domain `consents_required`; validation: name ≤200, phone ≤20 req, email pattern ≤254 req, password ≥8 ≤128 | Member portal signup | VERIFIED |
| 5.2 | POST | `/login` | `{ email, password }` | `{ member: MemberDto, accessToken, expiresAtUtc }` | 401 `unauthorized`; 404 `company_not_found` | Member portal login | VERIFIED |

**Member tokens have NO refresh endpoint** — short-lived access token; expiry → re-login
(`MemberTokenIssuer.cs` comment). `expiresAtUtc` is provided at issue time.

## 6. Member Self-Service — `/api/v1/member` (role: `Member`)

Every handler resolves tenant from `company_id` claim first; failure → env 404 `company_not_found`.

### Profile & account

| # | Method | Route | Body | Response | Errors | Status |
|---|--------|-------|------|----------|--------|--------|
| 6.1 | GET | `/me` | — | `MemberDto { customerId, fullName, phone, email?, level, levelLabel, memberCode?, emailVerified, phoneVerified, defaultReminderMinutes?, createdAtUtc }` | — | VERIFIED |
| 6.2 | PUT | `/me` | `{ fullName, email?, defaultReminderMinutes? }` | MemberDto | validation | VERIFIED |
| 6.3 | DELETE | `/me` | `{ password }` | success msg; hard delete | 401 wrong password | VERIFIED |
| 6.4 | POST | `/otp/request` | `{ purpose: "email"\|"phone" }` | msg | — | VERIFIED |
| 6.5 | POST | `/otp/verify` | `{ purpose, code }` | MemberDto (verified flags flip) | `otp_expired`, `otp_invalid`, `otp_not_found` | VERIFIED |

### Appointments & packages

| # | Method | Route | Body | Response | Errors | Status |
|---|--------|-------|------|----------|--------|--------|
| 6.6 | GET | `/appointments` | — | `MemberAppointmentDto[] { id, date, startTime, boatClass, boatName?, instructorName?, status, note?, reminderMinutes?, usedPackage, crewmates: maskedNames[] }` | — | VERIFIED |
| 6.7 | POST | `/appointments` | `MemberBookingRequest { date, time, boatClass?, note?, reminderMinutes?, usePackage, acceptedConsents?[] }` (identity from profile) | BookAppointmentResponse (201) | env `invalid_date`,`invalid_time`; domain same as 4.7 minus `guest_class_restricted`; `no_package_sessions` when package exhausted (verify at impl) | VERIFIED |
| 6.8 | POST | `/appointments/{id}/cancel` | — | msg "Randevunuz iptal edildi." | 403 `forbidden` (not owner); package auto-refund | VERIFIED |
| 6.9 | GET | `/packages` | — | `CustomerPackageDto[] { id, customerId, packageName, totalSessions, remainingSessions, assignedAtUtc }` | — | VERIFIED |

### Consents / Cards

| # | Method | Route | Body | Response | Errors | Status |
|---|--------|-------|------|----------|--------|--------|
| 6.10 | GET | `/consents` | — | ConsentStateDto[] (with member state incl. acceptedAtUtc/ipAddress) | — | VERIFIED |
| 6.11 | POST | `/consents` | `{ entries: [{key, accepted}] }` | ConsentStateDto[] | `unknown_consent`, `consent_required` (rejecting mandatory) | VERIFIED |
| 6.12 | GET | `/cards` | — | `CardDto[] { type:"Multisport"\|"Meditopia", cardNumber?, companyName, expiryDate?, status:"Active"\|"Passive"\|"Expired", hasPhoto, photoBase64?, photoContentType?, updatedAtUtc }` | — | VERIFIED |
| 6.13 | PUT | `/cards` | `{ type, cardNumber?, companyName, expiryDate?, status, photoBase64?, photoContentType? }` | CardDto | `invalid_card_type`, `invalid_date`, `card_number_required` (Multisport digits-only) | VERIFIED |

### Friends & chat (SignalR `/hubs/chat`)

| # | Method | Route | Body | Response | Errors | Status |
|---|--------|-------|------|----------|--------|--------|
| 6.14 | GET | `/code` | — | `{ memberCode }` (generated lazily) | — | VERIFIED |
| 6.15 | GET | `/friends` | — | `FriendDto[] { friendshipId, customerId, fullName, memberCode?, level, direction:"incoming"\|"outgoing"\|"friend", status:"Pending"\|"Accepted", unreadCount }` | — | VERIFIED |
| 6.16 | POST | `/friends` | `{ memberCode }` | FriendDto ("outgoing") | `code_not_found`, `cannot_friend_self`, `already_friends`, `request_pending` | VERIFIED |
| 6.17 | POST | `/friends/{id}/accept` | — | msg | 403 `forbidden` (only addressee) | VERIFIED |
| 6.18 | POST | `/friends/{id}/reject` | — | msg | 403 `forbidden` | VERIFIED |
| 6.19 | GET | `/messages/{friendCustomerId}` | — | `MessageDto[] { id, senderId, recipientId, body, sentAtUtc, read }` (marks incoming read) | `not_friends` | VERIFIED |
| 6.20 | POST | `/messages/{friendCustomerId}` | `{ body }` | MessageDto | `empty_message`, `message_too_long`, `not_friends` | VERIFIED |

Hub: `[Authorize(Roles="Member")]`, event `"message"` pushes MessageDto; user key
`{company_id}:{sub}`; websocket auth via `?access_token=`; send stays REST.

### Feed

| # | Method | Route | Body | Response | Errors | Status |
|---|--------|-------|------|----------|--------|--------|
| 6.21 | GET | `/feed` | — | `PostDto[] { id, authorName, authorCustomerId?, isClubPost, body, mediaKind:"None"\|"Image"\|"Video", mediaContentType?, isEvent, eventTitle?, eventDate?, likeCount, likedByMe, commentCount, participantCount, joinedByMe, followingAuthor, isMine, createdAtUtc }` (latest 50) | — | VERIFIED |
| 6.22 | POST | `/feed` | `{ body, mediaBase64?, mediaContentType?, isEvent, eventTitle?, eventDate? }` | `{ postId }` | `media_invalid_type`, `media_too_large` (img ≤5MB jpg/png/gif/webp; video ≤25MB mp4/webm/mov), `invalid_date` | VERIFIED |
| 6.23 | GET | `/feed/{id}/media` | — | `{ base64, contentType }` | 404 env `no_media` | VERIFIED |
| 6.24 | POST | `/feed/{id}/delete` | — | msg | `forbidden` (own only) | VERIFIED |
| 6.25 | POST | `/feed/{id}/like` | — | `ToggleResult { active, count }` | — | VERIFIED |
| 6.26 | GET | `/feed/{id}/comments` | — | `CommentDto[] { id, authorName, body, createdAtUtc }` | — | VERIFIED |
| 6.27 | POST | `/feed/{id}/comments` | `{ body }` | CommentDto | — | VERIFIED |
| 6.28 | POST | `/feed/{id}/join` | — | ToggleResult | `not_event` | VERIFIED |
| 6.29 | GET | `/feed/{id}/participants` | — | `ParticipantDto[] { fullName, level, joinedAtUtc }` | — | VERIFIED |
| 6.30 | POST | `/follow/{customerId}` | — | ToggleResult (count = follower count of target) | — | VERIFIED |

## 7. Company Panel — `/api/v1/company` (role: `CompanyAdmin`)

All handlers resolve own company from claim; failure → env 404 `company_not_found`.

### General / dashboard

| # | Method | Route | Response | Status |
|---|--------|-------|----------|--------|
| 7.1 | GET | `/site` | `{ name, subdomain, siteUrl, mockPath }` | VERIFIED |
| 7.2 | GET | `/stats` | `CompanyStatsDto { memberCount, membersWithAccount, appointmentsToday, appointmentsThisMonth, thisMonthByStatus: Record<status,int>, sessionsToday, activePackageBalances, totalRemainingSessions, next7Days: [{date, appointments}] }` | VERIFIED |

### Appointments & sessions

| # | Method | Route | Query/Body | Response | Errors | Status |
|---|--------|-------|-----------|----------|--------|--------|
| 7.3 | GET | `/appointments` | `?date=yyyy-MM-dd` (optional; **omit = ALL ever**) | `AppointmentDto[] { id, sessionId, customerName, customerPhone, customerEmail?, customerLevel, date, startTime, boatClass, teammateName?, note?, status, reminderMinutes?, createdAtUtc }` | env `invalid_date` | VERIFIED |
| 7.4 | POST | `/appointments/{id}/status` | `{ status: "Pending"\|"Confirmed"\|"Cancelled"\|"Completed" }` | msg; cancel auto-refunds package, un-cancel re-deducts | invalid transitions → domain error | VERIFIED |
| 7.5 | GET | `/sessions` | `?date=` (**required valid**) | `SessionDto[] { id, date, startTime, boatClass, level, capacity, memberCount, boatName?, instructorName?, members: [{appointmentId, fullName, phone, level, status}] }` | env `invalid_date` | VERIFIED |
| 7.6 | POST | `/sessions/{id}/assign` | `{ boatId?, instructorId? }` | SessionDto | `boat_taken`, `instructor_busy`, 404 notfound | VERIFIED |

### Members & logs

| # | Method | Route | Body/Query | Response | Errors | Status |
|---|--------|-------|-----------|----------|--------|--------|
| 7.7 | GET | `/customers` | — | `CustomerDto[] { id, fullName, phone, email?, level, createdAtUtc }` (name-sorted) | — | VERIFIED |
| 7.8 | POST | `/customers` | `{ fullName, phone, email?, level }` | CustomerDto (201) | `phone_taken` | VERIFIED |
| 7.9 | POST | `/customers/{id}/level` | `{ level: 0–10 }` | CustomerDto | — | VERIFIED |
| 7.10 | POST | `/customers/{id}/packages` | `{ lessonPackageId }` | `CustomerPackageDto { id, customerId, packageName, totalSessions, remainingSessions, assignedAtUtc }` | `invalid_package` | VERIFIED |
| 7.11 | GET | `/customers/{id}/packages` | — | `CustomerPackageBalanceDto[]` | — | VERIFIED |
| 7.12 | GET | `/package-balances` | — | `CustomerPackageBalanceDto[] { id, customerId, customerName, packageName, totalSessions, remainingSessions, usedSessions, assignedAtUtc }` | — | VERIFIED |
| 7.13 | GET | `/customers/{id}/logs` | `?take=` (≤500, dflt 100) | `MemberLogDto[] { customerId, customerName, event, details?, atUtc }` | — | VERIFIED |
| 7.14 | GET | `/logs` | `?take=` | MemberLogDto[] | — | VERIFIED |

Events seen in logs (`MemberEvents`): MEMBER_REGISTERED, MEMBER_LOGIN, PROFILE_UPDATED,
ACCOUNT_DELETED, APPOINTMENT_BOOKED, APPOINTMENT_CANCELLED, PACKAGE_ASSIGNED, PACKAGE_DEDUCTED,
PACKAGE_REFUNDED, LEVEL_CHANGED (+ CONSENTS_SUBMITTED, FRIEND_REQUEST_SENT, CARD_UPDATED).

### Resources

| # | Method | Route | Body | Response | Errors | Status |
|---|--------|-------|------|----------|--------|--------|
| 7.15 | GET | `/instructors` | — | `InstructorDto[] { id, fullName, phone?, email?, isActive }` | — | VERIFIED |
| 7.16 | POST | `/instructors` | `{ fullName, phone?, email?, isActive? }` | InstructorDto (201) | — | VERIFIED |
| 7.17 | PUT | `/instructors/{id}` | same (isActive required-effective, dflt true) | InstructorDto | 404 | VERIFIED |
| 7.18 | GET | `/boats` | — | `BoatDto[] { id, name, class:"1x"\|"2x"\|"4x", capacity:1\|2\|4, isActive }` | — | VERIFIED |
| 7.19 | POST | `/boats` | `{ name, boatClass }` | BoatDto (201) | `invalid_name`, invalid class parse | VERIFIED |
| 7.20 | PUT | `/boats/{id}` | `{ name, boatClass, isActive? }` | BoatDto | 404 | VERIFIED |
| 7.21 | GET | `/packages` | — | `PackageDto[] { id, name, description?, sessionCount, price, isActive }` | — | VERIFIED |
| 7.22 | POST | `/packages` | `{ name, description?, sessionCount, price, isActive? }` | PackageDto (201) | — | VERIFIED |
| 7.23 | PUT | `/packages/{id}` | same | PackageDto | 404 | VERIFIED |

### Settings & closed dates

| # | Method | Route | Body | Response | Errors | Status |
|---|--------|-------|------|----------|--------|--------|
| 7.24 | GET | `/settings` | — | `CompanySettingsDto { openingTime, closingTime, slotMinutes, openDays:number[0=Sun..6], minNoticeHours, maxAdvanceDays, reminderOptions:number[], defaultReminderMinutes, timeZoneId }` | — | VERIFIED |
| 7.25 | PUT | `/settings` | same shape | CompanySettingsDto | `invalid_time` (HH:mm strict) | VERIFIED |
| 7.26 | GET | `/closed-dates` | — | `ClosedDateDto[] { id, date, reason? }` | — | VERIFIED |
| 7.27 | POST | `/closed-dates` | `{ date, reason? }` | ClosedDateDto | env `invalid_date` | VERIFIED |
| 7.28 | DELETE | `/closed-dates/{date}` | — | msg | env `invalid_date` | VERIFIED |

### Feed moderation

| # | Method | Route | Response | Errors | Status |
|---|--------|-------|----------|--------|--------|
| 7.29 | GET | `/feed` | PostDto[] (viewer=null: no likedByMe/joinedByMe/isMine flags true) | — | VERIFIED |
| 7.30 | POST | `/feed` | `{ postId }` — club-author post (authorCustomerId null) | media codes | VERIFIED |
| 7.31 | POST | `/feed/{id}/delete` | msg — deletes ANY post | 404 | VERIFIED |
| 7.32 | GET | `/feed/{id}/media` | PostMediaDto \| 404 `no_media` | — | VERIFIED |
| 7.33 | GET | `/feed/{id}/comments` | CommentDto[] | — | VERIFIED |
| 7.34 | GET | `/feed/{id}/participants` | ParticipantDto[] | — | VERIFIED |

### Panel users (double-guarded: role check + DB re-validation per call)

| # | Method | Route | Body | Response | Errors | Status |
|---|--------|-------|------|----------|--------|--------|
| 7.35 | GET | `/users` | — | `CompanyUserDto[] { id, email, role:"CompanyAdmin"\|"Employee", status:"Active"\|"Deactivated", createdAtUtc }` | `caller_not_found`, `caller_deactivated`, `forbidden` | VERIFIED |
| 7.36 | POST | `/users` | `{ email, password, role }` | CompanyUserDto (201) | `email_already_registered`, `invalid_role` (PlatformAdmin never assignable), guard codes | VERIFIED |
| 7.37 | POST | `/users/{id}/role` | `{ role }` | CompanyUserDto | `user_not_found`, `forbidden` (cross-company), `cannot_demote_self`, `invalid_role` | VERIFIED |

## 8. System

| Route | Kind | UI | Status |
|---|---|---|---|
| `GET /health` | liveness incl. Postgres | NO_UI | VERIFIED |
| `GET /metrics` | Prometheus | NO_UI | VERIFIED |
| `GET /openapi/v1.json`, `/scalar/v1` | Development only | NO_UI | VERIFIED |
| `/site/{subdomain}`, `/` | server-rendered HTML club page | NO_UI (backend-owned) | VERIFIED |
| `wss /hubs/chat` | SignalR hub, Member role, `?access_token=` | Member portal chat | VERIFIED |

## Error-code catalog (verified occurrences)

`unauthorized`, `forbidden`, `validation_error`, `unexpected_error`, `company_not_found`,
`company_name_taken`, `email_already_registered`, `invalid_date`, `invalid_time`,
`invalid_name`, `phone_taken`, `member_exists`, `email_taken`, `consents_required`,
`consent_required`, `unknown_consent`, `guest_class_restricted`, `no_2x_partner_available`,
`already_booked`, `closed_date`, `too_soon`, `too_far`, `invalid_slot`, `slot_full`, `session_full`,
`boat_class_unavailable`, `boat_class_restricted` (verify-at-impl), `invalid_reminder`,
`otp_expired`, `otp_invalid`, `otp_not_found`, `invalid_card_type`, `card_number_required`,
`code_not_found`, `code_generation_failed`, `cannot_friend_self`, `already_friends`,
`request_pending`, `not_friends`, `empty_message`, `message_too_long`, `no_media`,
`media_invalid_type`, `media_too_large`, `not_event`, `invalid_package`, `boat_taken`,
`instructor_busy`, `invalid_role`, `user_not_found`, `caller_not_found`, `caller_deactivated`,
`cannot_demote_self`, `concurrency_conflict`, `code_generation_failed`.

Codes marked *verify-at-impl* were inferred from docs and must be confirmed in the exact
handler before wiring a specific UI branch; unknown codes always fall back to server message.
