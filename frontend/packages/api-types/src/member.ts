/** Member self-service — sources: MemberEndpoints.cs + Scheduling.Application/Members/*.cs. */
import type {
  FriendDirection,
  FriendshipStatus,
  MembershipCardType,
  OtpPurpose,
  PostMediaKind,
} from "./common";
import type { DateStr, TimeStr, TimestampIso } from "./envelope";
import type { BookAppointmentResponse, ConsentStateDto } from "./public";

/** MemberCommon.cs MemberDto (also returned by register/login as `member`). */
export interface MemberDto {
  readonly customerId: string;
  readonly fullName: string;
  readonly phone: string;
  readonly email: string | null;
  readonly level: number;
  readonly levelLabel: string;
  readonly memberCode: string | null;
  readonly emailVerified: boolean;
  readonly phoneVerified: boolean;
  readonly defaultReminderMinutes: number | null;
  readonly createdAtUtc: TimestampIso;
}

/** Anonymous object from member register/login endpoints. */
export interface MemberAuthResponse {
  readonly member: MemberDto;
  readonly accessToken: string;
  /** UTC instant after which re-login is required — members have NO refresh endpoint. */
  readonly expiresAtUtc: TimestampIso;
}

export interface MemberUpdateRequest {
  readonly fullName: string;
  readonly email?: string | null;
  readonly defaultReminderMinutes?: number | null;
}

export interface MemberDeleteRequest {
  readonly password: string;
}

export interface OtpRequestDto {
  readonly purpose: OtpPurpose;
}

export interface OtpVerifyRequest {
  readonly purpose: OtpPurpose;
  readonly code: string;
}

/** GetMemberAppointmentsQuery.cs — crewmates are KVKK-masked names ("Veli D."). */
export interface MemberAppointmentDto {
  readonly id: string;
  readonly date: DateStr;
  readonly startTime: TimeStr;
  readonly boatClass: string;
  readonly boatName: string | null;
  readonly instructorName: string | null;
  readonly status: string;
  readonly note: string | null;
  readonly reminderMinutes: number | null;
  readonly usedPackage: boolean;
  readonly crewmates: readonly string[];
}

/** MemberBookingRequest — identity comes from the authenticated profile server-side. */
export interface MemberBookingRequest {
  readonly date: DateStr;
  readonly time: TimeStr;
  readonly boatClass?: string | null;
  readonly note?: string | null;
  readonly reminderMinutes?: number | null;
  readonly usePackage: boolean;
  readonly acceptedConsents?: readonly string[] | null;
}

export type MemberBookingResponse = BookAppointmentResponse;

/** GetMemberPackagesQuery → CustomerPackageDto (Members/MemberManagementCommands.cs). */
export interface CustomerPackageDto {
  readonly id: string;
  readonly customerId: string;
  readonly packageName: string;
  readonly totalSessions: number;
  readonly remainingSessions: number;
  readonly assignedAtUtc: TimestampIso;
}

export interface ConsentEntry {
  readonly key: string;
  readonly accepted: boolean;
}

export interface ConsentSubmitRequest {
  readonly entries: readonly ConsentEntry[];
}

export type MyConsentsResponse = readonly ConsentStateDto[];

/** CardCommands.cs CardDto / CardUpsertRequest. */
export interface CardDto {
  readonly type: MembershipCardType;
  readonly cardNumber: string | null;
  readonly companyName: string;
  readonly expiryDate: DateStr | null;
  readonly status: "Active" | "Passive" | "Expired";
  readonly hasPhoto: boolean;
  readonly photoBase64: string | null;
  readonly photoContentType: string | null;
  readonly updatedAtUtc: TimestampIso;
}

export interface CardUpsertRequest {
  readonly type: string;
  /** Required for Multisport (digits only); ignored for Meditopia. */
  readonly cardNumber?: string | null;
  readonly companyName: string;
  readonly expiryDate?: string | null;
  /** Server coerces unknown/"Expired" to Active; send "Active"|"Passive". */
  readonly status: string;
  readonly photoBase64?: string | null;
  readonly photoContentType?: string | null;
}

/** SocialCommands.cs FriendDto. */
export interface FriendDto {
  readonly friendshipId: string;
  readonly customerId: string;
  readonly fullName: string;
  readonly memberCode: string | null;
  readonly level: number;
  readonly direction: FriendDirection;
  readonly status: FriendshipStatus;
  readonly unreadCount: number;
}

export interface FriendAddRequest {
  readonly memberCode: string;
}

/** MessageCommands.cs MessageDto. */
export interface MessageDto {
  readonly id: string;
  readonly senderId: string;
  readonly recipientId: string;
  readonly body: string;
  readonly sentAtUtc: TimestampIso;
  readonly read: boolean;
}

export interface MessageSendRequest {
  readonly body: string;
}

/** CommunityCommands.cs PostDto. */
export interface PostDto {
  readonly id: string;
  readonly authorName: string;
  readonly authorCustomerId: string | null;
  readonly isClubPost: boolean;
  readonly body: string;
  readonly mediaKind: PostMediaKind;
  readonly mediaContentType: string | null;
  readonly isEvent: boolean;
  readonly eventTitle: string | null;
  readonly eventDate: DateStr | null;
  readonly likeCount: number;
  readonly likedByMe: boolean;
  readonly commentCount: number;
  readonly participantCount: number;
  readonly joinedByMe: boolean;
  readonly followingAuthor: boolean;
  readonly isMine: boolean;
  readonly createdAtUtc: TimestampIso;
}

export interface PostCreateRequest {
  readonly body: string;
  readonly mediaBase64?: string | null;
  readonly mediaContentType?: string | null;
  readonly isEvent: boolean;
  readonly eventTitle?: string | null;
  readonly eventDate?: string | null;
}

export interface PostMediaDto {
  readonly base64: string;
  readonly contentType: string;
}

export interface CommentDto {
  readonly id: string;
  readonly authorName: string;
  readonly body: string;
  readonly createdAtUtc: TimestampIso;
}

export interface CommentCreateRequest {
  readonly body: string;
}

export interface ParticipantDto {
  readonly fullName: string;
  readonly level: number;
  readonly joinedAtUtc: TimestampIso;
}

/** CommunityCommands.cs ToggleResult (like/join/follow). */
export interface ToggleResult {
  readonly active: boolean;
  readonly count: number;
}

export interface MemberCodeResponse {
  readonly memberCode: string;
}
