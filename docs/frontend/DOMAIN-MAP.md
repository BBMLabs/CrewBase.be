# CrewBase — Domain Map (Backend → Frontend)

> Verified against source. The five "empty" modules (`Clubs`, `Memberships`, `Notifications`,
> `Packages`, `Reporting`) contain only `AssemblyMarker.cs` / nothing — they are scaffolding.
> All real domain logic lives in **Identity** and **Scheduling**.

## Module → HTTP → Frontend capability

```text
RowingClub.Identity  (src/Modules/Identity)
│  Companies, CompanyUsers, Users/Credentials, Login/Refresh/Sessions,
│  PasswordReset, EmailVerification(OTP), Platform, Audit(structural log only)
│
├──→ POST /api/v1/auth/*                      → Panel: auth flows (login, refresh, logout-all,
│                                               forgot/reset password, email OTP, signup)
├──→ GET/POST /api/v1/platform/*              → (future apps/platform-admin) companies + stats
├──→ GET /api/v1/admin/*                      → placeholder text endpoints — NO UI
└──→ (company user mgmt surfaced via) /api/v1/company/users* → Panel: Ayarlar > Kullanıcılar

RowingClub.Scheduling  (src/Modules/Scheduling)
│  Domain: Appointments(+Status), Boats(BoatClass), Instructors, Sessions,
│          Customers(+RowingLevels), Packages(LessonPackage,CustomerPackage),
│          Consents(Catalog), Cards(MembershipCard), Logs(MemberLog),
│          Social(Friendship, DirectMessage), Community(Post,Like,Comment,
│          Participation,Follow), Settings(CompanySettings, ClosedDates)
│
├── Application/Panel        → /api/v1/company/{site,stats,appointments,sessions,customers,
│                              package*,logs,instructors,boats,packages,settings,closed-dates}
│                              → Company Panel features (below)
├── Application/Members      → /api/v1/member/{me,otp,appointments,packages,consents,cards,
│                              code,friends,messages} + public member register/login
│                              → Member Portal features (below)
├── Application/Community    → /api/v1/{member,company}/feed* , /member/follow
│                              → Member feed; Panel moderation
├── Application/Booking      → POST public/member appointments (+validator)
│                              → Booking workflows (all three frontends)
├── Application/Availability → GET public availability → Slot pickers
└── Application/PublicOptions→ GET public options → Public booking config

SignalR ChatHub (/hubs/chat) → Member Portal realtime chat channel (send stays REST)
ReminderWorker (background)  → e-mail reminders; no HTTP surface; NO UI
```

## Company Panel feature tree (derived from `/api/v1/company`)

```text
Firma Paneli (CompanyAdmin)
├── Kimlik Doğrulama            auth: login / refresh / logout(-all) / forgot-reset pw /
│                               email OTP / kulüp kaydı (companies/register)
├── Genel Bakış (Dashboard)     GET /stats, GET /appointments?date=today, GET /sessions?date=today
├── Gün Programı (Scheduling ops)
│   ├── Randevu Listesi         GET /appointments?date= ; status workflow POST /{id}/status
│   └── Seanslar                GET /sessions?date= ; boat/instructor assignment POST /{id}/assign
│                               (conflicts: boat_taken / instructor_busy)
├── Üyeler
│   ├── Üye Dizini              GET/POST /customers ; level set POST /{id}/level
│   ├── Paket İşlemleri         assign/list per member ; GET /package-balances
│   └── Hareket Geçmişi         GET /customers/{id}/logs ; GET /logs
├── Paketler                    CRUD /packages (catalog) 
├── Kaynaklar                   CRUD /boats, /instructors
├── Ayarlar                     GET/PUT /settings (working hours, slot, open days,
│                               notice window, reminders, timezone) + closed dates CRUD
├── Kulüp Akışı (moderasyon)    GET/POST /feed, delete any, media/comments/participants read
└── Kullanıcılar                GET/POST /users, POST /users/{id}/role (double-guarded backend)
```

## Member Portal feature tree (derived from `/api/v1/member` + member auth)

```text
Üye Portalı (Member)
├── Giriş / Kayıt               POST /public/{subdomain}/members/login | /register
├── Ana Sayfa (Rezervasyon)     GET public info+options+consents+availability ; POST /member/appointments
├── Randevularım                GET /appointments ; cancel POST /{id}/cancel
├── Paketlerim                  GET /packages (balance shown on booking via usePackage)
├── Profil                      GET/PUT/DELETE /me ; OTP request/verify
├── Beyanlarım                  GET/POST /consents
├── Üyelik Kartları             GET/PUT /cards (Multisport/Meditopia)
├── Arkadaşlar & Sohbet         /code, /friends(+accept|reject), /messages, SignalR receive
└── Kulüp Akışı                 /feed + media/comments/participants/like/join/follow
```

## Public Site feature tree (anonymous)

```text
Kulüp Sitesi (Public)
├── Kulüp Tanıtımı              GET /info (+backend-rendered HTML page exists at /site/{sub})
├── Müsaitlik                    GET /options, GET /availability?date&boatClass[&phone]
└── Misafir Rezervasyonu        GET /consents ; POST /appointments (guest constraints:
                                4x-only, consents every time, no package use)
```

## Cross-cutting domain rules the UI must respect (verified)

- **Seviye (0–10):** only company admin sets it; members see read-only; session grouping is by
  date/slot/class/**level**. Labels from `RowingLevels.Labels` (11 Turkish strings).
- **Tekne kapasitesi:** 1x=1, 2x=2, 4x=4. Full sessions reject (`session_full`, surfaced as
  `slot_full` at slot level). Same slot+class+level groups into free seats first.
- **Beyanlar:** Booking scope (swim/health/rules/kvkk) mandatory: guests EVERY booking,
  registered members once. Member scope: health-data mandatory, photo/marketing optional
  revocable. Rejecting a required consent → `consent_required`.
- **Paket ekonomisi:** cancel → auto refund; un-cancel → re-deduct; guest bookings never deduct.
- **KVKK masking:** crewmate names masked (`Veli D.`); phone/email of co-members never exposed
  to members (panel sees full data).
- **Zaman:** club-local wall times with explicit `timeZoneId` in settings; API exchanges naive
  local strings for dates/times and UTC ISO instants for timestamps. UI never silently converts.
