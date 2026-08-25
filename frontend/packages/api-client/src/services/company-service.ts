/** Company panel service — full verified /api/v1/company surface. */
import type {
  AppointmentDto,
  AssignSessionRequest,
  BoatDto,
  BoatRequest,
  ClosedDateDto,
  ClosedDateRequest,
  ClubPostCreateRequest,
  CommentDto,
  CompanySettingsDto,
  CompanySiteInfo,
  CompanyStatsDto,
  CompanyUserDto,
  CreateCompanyUserRequest,
  CreateMemberRequest,
  CustomerDto,
  CustomerPackageBalanceDto,
  CustomerPackageDto,
  InstructorDto,
  InstructorRequest,
  MemberLogDto,
  PackageDto,
  PackageRequest,
  ParticipantDto,
  PostCreatedResponse,
  PostDto,
  PostMediaDto,
  SessionDto,
} from "@crewbase/api-types";
import type { AppointmentStatus } from "@crewbase/api-types";
import type { ApiClient } from "../http/api-client";

export interface CompanyService {
  site(): Promise<CompanySiteInfo>;
  stats(): Promise<CompanyStatsDto>;

  appointments(date?: string): Promise<readonly AppointmentDto[]>;
  setAppointmentStatus(appointmentId: string, status: AppointmentStatus): Promise<void>;
  sessions(date: string): Promise<readonly SessionDto[]>;
  assignSession(sessionId: string, request: AssignSessionRequest): Promise<SessionDto>;

  members(): Promise<readonly CustomerDto[]>;
  createMember(request: CreateMemberRequest): Promise<CustomerDto>;
  setMemberLevel(customerId: string, level: number): Promise<CustomerDto>;
  assignPackage(customerId: string, lessonPackageId: string): Promise<CustomerPackageDto>;
  memberPackages(customerId: string): Promise<readonly CustomerPackageBalanceDto[]>;
  packageBalances(): Promise<readonly CustomerPackageBalanceDto[]>;
  memberLogs(customerId: string, take?: number): Promise<readonly MemberLogDto[]>;
  recentLogs(take?: number): Promise<readonly MemberLogDto[]>;

  boats(): Promise<readonly BoatDto[]>;
  createBoat(request: BoatRequest): Promise<BoatDto>;
  updateBoat(boatId: string, request: BoatRequest): Promise<BoatDto>;

  instructors(): Promise<readonly InstructorDto[]>;
  createInstructor(request: InstructorRequest): Promise<InstructorDto>;
  updateInstructor(instructorId: string, request: InstructorRequest): Promise<InstructorDto>;

  packages(): Promise<readonly PackageDto[]>;
  createPackage(request: PackageRequest): Promise<PackageDto>;
  updatePackage(packageId: string, request: PackageRequest): Promise<PackageDto>;

  settings(): Promise<CompanySettingsDto>;
  updateSettings(request: CompanySettingsDto): Promise<CompanySettingsDto>;
  closedDates(): Promise<readonly ClosedDateDto[]>;
  addClosedDate(request: ClosedDateRequest): Promise<ClosedDateDto>;
  removeClosedDate(date: string): Promise<void>;

  feed(): Promise<readonly PostDto[]>;
  createClubPost(request: ClubPostCreateRequest): Promise<PostCreatedResponse>;
  deleteAnyPost(postId: string): Promise<void>;
  postMedia(postId: string): Promise<PostMediaDto | null>;
  postComments(postId: string): Promise<readonly CommentDto[]>;
  postParticipants(postId: string): Promise<readonly ParticipantDto[]>;

  users(): Promise<readonly CompanyUserDto[]>;
  createUser(request: CreateCompanyUserRequest): Promise<CompanyUserDto>;
  changeUserRole(userId: string, role: "CompanyAdmin" | "Employee"): Promise<CompanyUserDto>;
}

export function createCompanyService(client: ApiClient): CompanyService {
  return {
    site() {
      return client.get("/api/v1/company/site");
    },
    stats() {
      return client.get("/api/v1/company/stats");
    },

    appointments(date) {
      return client.get("/api/v1/company/appointments", { query: { date } });
    },
    setAppointmentStatus(appointmentId, status) {
      return client
        .post(`/api/v1/company/appointments/${appointmentId}/status`, { status })
        .then(() => undefined);
    },
    sessions(date) {
      return client.get("/api/v1/company/sessions", { query: { date } });
    },
    assignSession(sessionId, request) {
      return client.post<AssignSessionRequest, SessionDto>(
        `/api/v1/company/sessions/${sessionId}/assign`,
        request,
      );
    },

    members() {
      return client.get("/api/v1/company/customers");
    },
    createMember(request) {
      return client.post<CreateMemberRequest, CustomerDto>("/api/v1/company/customers", request);
    },
    setMemberLevel(customerId, level) {
      return client.post(
        `/api/v1/company/customers/${customerId}/level`,
        { level } satisfies { level: number },
      ) as Promise<CustomerDto>;
    },
    assignPackage(customerId, lessonPackageId) {
      return client.post(`/api/v1/company/customers/${customerId}/packages`, {
        lessonPackageId,
      }) as Promise<CustomerPackageDto>;
    },
    memberPackages(customerId) {
      return client.get(`/api/v1/company/customers/${customerId}/packages`);
    },
    packageBalances() {
      return client.get("/api/v1/company/package-balances");
    },
    memberLogs(customerId, take) {
      return client.get(`/api/v1/company/customers/${customerId}/logs`, { query: { take } });
    },
    recentLogs(take) {
      return client.get("/api/v1/company/logs", { query: { take } });
    },

    boats() {
      return client.get("/api/v1/company/boats");
    },
    createBoat(request) {
      return client.post("/api/v1/company/boats", request) as Promise<BoatDto>;
    },
    updateBoat(boatId, request) {
      return client.put(`/api/v1/company/boats/${boatId}`, request) as Promise<BoatDto>;
    },

    instructors() {
      return client.get("/api/v1/company/instructors");
    },
    createInstructor(request) {
      return client.post("/api/v1/company/instructors", request) as Promise<InstructorDto>;
    },
    updateInstructor(instructorId, request) {
      return client.put(`/api/v1/company/instructors/${instructorId}`, request) as Promise<InstructorDto>;
    },

    packages() {
      return client.get("/api/v1/company/packages");
    },
    createPackage(request) {
      return client.post("/api/v1/company/packages", request) as Promise<PackageDto>;
    },
    updatePackage(packageId, request) {
      return client.put(`/api/v1/company/packages/${packageId}`, request) as Promise<PackageDto>;
    },

    settings() {
      return client.get("/api/v1/company/settings");
    },
    updateSettings(request) {
      return client.put("/api/v1/company/settings", request) as Promise<CompanySettingsDto>;
    },
    closedDates() {
      return client.get("/api/v1/company/closed-dates");
    },
    addClosedDate(request) {
      return client.post("/api/v1/company/closed-dates", request) as Promise<ClosedDateDto>;
    },
    removeClosedDate(date) {
      return client.delete(`/api/v1/company/closed-dates/${encodeURIComponent(date)}`).then(() => undefined);
    },

    feed() {
      return client.get("/api/v1/company/feed");
    },
    createClubPost(request) {
      return client.post("/api/v1/company/feed", request) as Promise<PostCreatedResponse>;
    },
    deleteAnyPost(postId) {
      return client.post(`/api/v1/company/feed/${postId}/delete`).then(() => undefined);
    },
    postMedia(postId) {
      // 404 no_media is a normal outcome for media-less posts — callers handle null.
      return client
        .get<PostMediaDto>(`/api/v1/company/feed/${postId}/media`)
        .catch((error: unknown) => {
          if (
            error instanceof Error &&
            "code" in error &&
            (error as { code?: string }).code === "no_media"
          ) {
            return null;
          }
          throw error;
        });
    },
    postComments(postId) {
      return client.get(`/api/v1/company/feed/${postId}/comments`);
    },
    postParticipants(postId) {
      return client.get(`/api/v1/company/feed/${postId}/participants`);
    },

    users() {
      return client.get("/api/v1/company/users");
    },
    createUser(request) {
      return client.post("/api/v1/company/users", request) as Promise<CompanyUserDto>;
    },
    changeUserRole(userId, role) {
      return client.post(`/api/v1/company/users/${userId}/role`, { role }) as Promise<CompanyUserDto>;
    },
  };
}
