# CrewBase — Feature Matrix

> Feature-level view. Every row is traceable: Feature → Endpoint(s) → Hook → Service →
> Guard → Tests. Status mirrors ENDPOINT-UI-MAP (PLANNED until the phase lands, then
> IMPLEMENTED / PARTIAL with reason).

## Company Panel (Phase 2)

| Feature | Endpoints | Hook(s) | Service | Guard | Tests |
|---|---|---|---|---|---|
| Giriş | POST /auth/login | useLogin | authApi.login | public | IT,E2E |
| Oturum yenileme | POST /auth/refresh | refresh queue (internal) | authApi.refresh | — | IT (incl. reuse→logout) |
| Çıkış / Tüm cihazlar | POST /auth/logout, /auth/logout-all | useLogout, useLogoutAll | authApi.* | session | IT |
| Şifremi unuttum / sıfırla | POST forgot-password, reset-password | useForgotPassword, useResetPassword | authApi.* | public | IT |
| E-posta doğrulama (OTP) | POST verify-email, send-verification-email | useVerifyEmail, useResendVerification | authApi.* | public | IT |
| Kulüp kaydı | POST /auth/companies/register | useRegisterCompany | authApi.registerCompany | public | IT |
| Genel Bakış | GET /company/stats (+today appointments/sessions for agenda) | useCompanyStats | companyApi.stats | CompanyAdmin | IT |
| Randevu gün listesi | GET /company/appointments?date | useAppointments(date) | companyApi.appointments | CompanyAdmin | IT |
| Randevu durum değiştirme | POST /appointments/{id}/status | useSetAppointmentStatus | companyApi.setAppointmentStatus | CompanyAdmin | IT (refund copy) |
| Seans gün listesi | GET /company/sessions?date | useSessions(date) | companyApi.sessions | CompanyAdmin | IT |
| Tekne/eğitmen atama | POST /sessions/{id}/assign + GET boats,instructors | useAssignSession | companyApi.assignSession | CompanyAdmin | IT (conflicts) |
| Üye dizini | GET/POST /customers | useMembers, useCreateMember | companyApi.members | CompanyAdmin | IT |
| Seviye atama | POST /customers/{id}/level | useSetMemberLevel | companyApi.setMemberLevel | CompanyAdmin | IT |
| Paket tanımlama | GET /packages + POST /customers/{id}/packages | useAssignPackage | companyApi.assignPackage | CompanyAdmin | IT |
| Paket bakiyeleri | GET /package-balances, GET /customers/{id}/packages | usePackageBalances | companyApi.packageBalances | CompanyAdmin | IT |
| Hareket geçmişi | GET /logs, GET /customers/{id}/logs | useMemberLogs | companyApi.logs | CompanyAdmin | IT |
| Tekneler | CRUD /boats | useBoats, useCreateBoat, useUpdateBoat | companyApi.boats | CompanyAdmin | IT |
| Eğitmenler | CRUD /instructors | useInstructors, ... | companyApi.instructors | CompanyAdmin | IT |
| Paket kataloğu | CRUD /packages | usePackages, ... | companyApi.packages | CompanyAdmin | IT |
| Kulüp ayarları | GET/PUT /settings | useSettings, useUpdateSettings | companyApi.settings | CompanyAdmin | IT |
| Kapalı günler | CRUD /closed-dates | useClosedDates, ... | companyApi.closedDates | CompanyAdmin | IT |
| Akış moderasyonu | GET/POST /feed, delete/media/comments/participants | useFeed(viewer:null), useModeratePost | companyApi.feed | CompanyAdmin | IT |
| Kullanıcı yönetimi | GET/POST /users, POST /users/{id}/role | useCompanyUsers, useCreateUser, useChangeUserRole | companyApi.users | CompanyAdmin | IT (guard errors) |
| Site bilgisi | GET /company/site | useCompanySite | companyApi.site | CompanyAdmin | IT |

## Member Portal (Phase 3)

| Feature | Endpoints | Hook(s) | Service | Guard | Tests |
|---|---|---|---|---|---|
| Üye girişi/kaydı | POST /public/{sub}/members/login·register | useMemberLogin, useMemberRegister | memberApi.auth | public+subdomain | IT,E2E |
| Rezervasyon akışı | GET public options/consents/availability + POST /member/appointments | useBookingWorkflow | publicApi.*, memberApi.book | Member | IT,E2E (slot_full) |
| Randevularım | GET /appointments, cancel | useMyAppointments, useCancelAppointment | memberApi.appointments | Member | IT |
| Paketlerim | GET /packages | useMyPackages | memberApi.packages | Member | IT |
| Profil | GET/PUT/DELETE /me, OTP pair | useProfile, useDeleteAccount, useOtp | memberApi.profile | Member | IT |
| Beyanlarım | GET/POST /consents | useMyConsents, useSubmitConsents | memberApi.consents | Member | IT |
| Kartlarım | GET/PUT /cards | useCards, useUpsertCard | memberApi.cards | Member | IT |
| Arkadaşlar | /code,/friends(+respond) | useFriends, useFriendActions | memberApi.friends | Member | IT |
| Sohbet | messages pair + SignalR hub | useConversation, useChatConnection (manager) | memberApi.messages + realtime | Member | IT + manager unit tests |
| Kulüp akışı | feed cluster | useFeed, useLikeToggle... | memberApi.feed | Member | IT |

## Public Site (Phase 4)

| Feature | Endpoints | Hook(s) | Service | Guard | Tests |
|---|---|---|---|---|---|
| Kulüp sayfası | GET info (+options for hero hours) | useClubInfo | publicApi.club | anonymous | IT |
| Müsaitlik | GET availability | useAvailabilityGrid | publicApi.availability | anonymous | IT |
| Misafir rezervasyonu | GET consents + POST appointments | useGuestBooking | publicApi.booking | anonymous | IT,E2E |

## Not built (documented boundaries)

- `/platform/*` — future apps/platform-admin.
- `/admin/*` placeholders — NOT_USER_FACING.
- 2FA endpoints — unmapped in backend (404).
- Employee panel — backend provides nothing real (BACKEND_INCOMPLETE).
