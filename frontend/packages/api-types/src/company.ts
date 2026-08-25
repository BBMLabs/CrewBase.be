/** Company panel — sources: CompanyPanelEndpoints.cs request records +
 *  Scheduling.Application/Panel/*.cs + Identity.Application/{CompanyUsers,Companies}. */
import type {
  AppointmentStatus,
  BoatClassLabel,
  UserStatus,
  PostMediaKind
} from "./common";
import type { DateStr, TimeStr, TimestampIso } from "./envelope";
import type { CommentDto, ParticipantDto, PostDto, PostMediaDto } from "./member";

/** GET /company/site — anonymous object in endpoint. */
export interface CompanySiteInfo {
  readonly name: string;
  readonly subdomain: string;
  readonly siteUrl: string;
  readonly mockPath: string;
}

/** GetCompanyStatsQuery.cs. */
export interface DailyCountDto {
  readonly date: DateStr;
  readonly appointments: number;
}

export interface CompanyStatsDto {
  readonly memberCount: number;
  readonly membersWithAccount: number;
  readonly appointmentsToday: number;
  readonly appointmentsThisMonth: number;
  /** key = AppointmentStatus name. */
  readonly thisMonthByStatus: Readonly<Record<string, number>>;
  readonly sessionsToday: number;
  readonly activePackageBalances: number;
  readonly totalRemainingSessions: number;
  readonly next7Days: readonly DailyCountDto[];
}

/** GetAppointmentsQuery.cs AppointmentDto. */
export interface AppointmentDto {
  readonly id: string;
  readonly sessionId: string;
  readonly customerName: string;
  readonly customerPhone: string;
  readonly customerEmail: string | null;
  readonly customerLevel: number;
  readonly date: DateStr;
  readonly startTime: TimeStr;
  readonly boatClass: BoatClassLabel;
  readonly note: string | null;
  readonly status: AppointmentStatus;
  readonly reminderMinutes: number | null;
  readonly createdAtUtc: TimestampIso;
}

export interface SetAppointmentStatusRequest {
  readonly status: AppointmentStatus;
}

/** SessionQueries.cs. */
export interface SessionMemberDto {
  readonly appointmentId: string;
  readonly fullName: string;
  readonly phone: string;
  readonly level: number;
  readonly status: string;
}

export interface SessionDto {
  readonly id: string;
  readonly date: DateStr;
  readonly startTime: TimeStr;
  readonly boatClass: BoatClassLabel;
  readonly level: number;
  readonly capacity: number;
  readonly memberCount: number;
  readonly boatName: string | null;
  readonly instructorName: string | null;
  readonly members: readonly SessionMemberDto[];
}

export interface AssignSessionRequest {
  readonly boatId?: string | null;
  readonly instructorId?: string | null;
}

/** GetCustomersQuery.cs CustomerDto. */
export interface CustomerDto {
  readonly id: string;
  readonly fullName: string;
  readonly phone: string;
  readonly email: string | null;
  readonly level: number;
  readonly createdAtUtc: TimestampIso;
}

export interface CreateMemberRequest {
  readonly fullName: string;
  readonly phone: string;
  readonly email?: string | null;
  readonly level: number;
}

export interface SetCustomerLevelRequest {
  readonly level: number;
}

/** MemberManagementCommands.cs. */
export interface AssignPackageRequest {
  readonly lessonPackageId: string;
}

export interface CustomerPackageBalanceDto {
  readonly id: string;
  readonly customerId: string;
  readonly customerName: string;
  readonly packageName: string;
  readonly totalSessions: number;
  readonly remainingSessions: number;
  readonly usedSessions: number;
  readonly assignedAtUtc: TimestampIso;
}

export interface MemberLogDto {
  readonly customerId: string;
  readonly customerName: string;
  readonly event: string;
  readonly details: string | null;
  readonly atUtc: TimestampIso;
}

/** InstructorCommands.cs / BoatCommands.cs / PackageCommands.cs. */
export interface InstructorDto {
  readonly id: string;
  readonly fullName: string;
  readonly phone: string | null;
  readonly email: string | null;
  readonly isActive: boolean;
}

export interface InstructorRequest {
  readonly fullName: string;
  readonly phone?: string | null;
  readonly email?: string | null;
  readonly isActive?: boolean | null;
}

export interface BoatDto {
  readonly id: string;
  readonly name: string;
  readonly class: BoatClassLabel;
  readonly capacity: number;
  readonly isActive: boolean;
}

export interface BoatRequest {
  readonly name: string;
  readonly boatClass: BoatClassLabel;
  readonly isActive?: boolean | null;
}

export interface PackageDto {
  readonly id: string;
  readonly name: string;
  readonly description: string | null;
  readonly sessionCount: number;
  readonly price: number;
  readonly isActive: boolean;
}

export interface PackageRequest {
  readonly name: string;
  readonly description?: string | null;
  readonly sessionCount: number;
  readonly price: number;
  readonly isActive?: boolean | null;
}

/** SettingsCommands.cs — openDays: 0=Sunday … 6=Saturday. */
export interface CompanySettingsDto {
  readonly openingTime: TimeStr;
  readonly closingTime: TimeStr;
  readonly slotMinutes: number;
  readonly openDays: readonly number[];
  readonly minNoticeHours: number;
  readonly maxAdvanceDays: number;
  readonly reminderOptions: readonly number[];
  readonly defaultReminderMinutes: number;
  readonly timeZoneId: string;
}

export type UpdateSettingsRequest = CompanySettingsDto;

/** ClosedDateCommands.cs. */
export interface ClosedDateDto {
  readonly id: string;
  readonly date: DateStr;
  readonly reason: string | null;
}

export interface ClosedDateRequest {
  readonly date: DateStr;
  readonly reason?: string | null;
}

export interface ClubPostCreateRequest {
  readonly body: string;
  readonly mediaBase64?: string | null;
  readonly mediaContentType?: string | null;
  readonly isEvent: boolean;
  readonly eventTitle?: string | null;
  readonly eventDate?: string | null;
}

export interface PostCreatedResponse {
  readonly postId: string;
}

/** CompanyPanelEndpoints.cs CreateCompanyUserRequest — PlatformAdmin is never assignable. */
export interface CreateCompanyUserRequest {
  readonly email: string;
  readonly password: string;
  readonly role: "CompanyAdmin" | "Employee";
}

export interface ChangeUserRoleRequest {
  readonly role: "CompanyAdmin" | "Employee";
}

export type { PostDto, CommentDto, ParticipantDto, PostMediaDto };
export type PanelFeedPostMediaKind = PostMediaKind;

/** CompanyUserCommands.cs CompanyUserDto (role/status are enum ToString). */
export interface CompanyUserDto {
  readonly id: string;
  readonly email: string;
  readonly role: "CompanyAdmin" | "Employee";
  readonly status: UserStatus;
  readonly createdAtUtc: TimestampIso;
}
