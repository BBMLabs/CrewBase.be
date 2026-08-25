/** Public site + member auth — sources: PublicSiteEndpoints.cs, MemberEndpoints.cs,
 *  Scheduling.Application/{PublicOptions,Availability,Booking,Members}. */
import type { BoatClassLabel, ConsentScope } from "./common";
import type { DateStr, TimeStr, TimestampIso } from "./envelope";

/** GET /public/{subdomain}/info — anonymous object in endpoint. */
export interface PublicCompanyInfo {
  readonly name: string;
  readonly subdomain: string;
  readonly siteUrl: string;
  readonly phone: string | null;
  readonly contactEmail: string | null;
  readonly address: string | null;
}

/** GetPublicOptionsQuery.cs. */
export interface BoatClassOptionDto {
  readonly value: BoatClassLabel;
  readonly label: string;
  readonly capacity: number;
}

export interface PackageOptionDto {
  readonly id: string;
  readonly name: string;
  readonly description: string | null;
  readonly sessionCount: number;
  readonly price: number;
}

export interface PublicOptionsDto {
  readonly openingTime: TimeStr;
  readonly closingTime: TimeStr;
  readonly slotMinutes: number;
  /** 0=Sunday … 6=Saturday (settings.IsOpenOn((DayOfWeek)d)). */
  readonly openDays: readonly number[];
  readonly minNoticeHours: number;
  readonly maxAdvanceDays: number;
  readonly reminderOptions: readonly number[];
  readonly defaultReminderMinutes: number;
  readonly boatClasses: readonly BoatClassOptionDto[];
  readonly packages: readonly PackageOptionDto[];
  readonly levelLabels: readonly string[];
}

/** ConsentCommands.cs ConsentStateDto — shared by public catalog & member status views.
 *  Icon values are emoji strings from ConsentCatalog.cs. */
export interface ConsentStateDto {
  readonly key: string;
  readonly title: string;
  readonly body: string;
  readonly scope: ConsentScope;
  readonly required: boolean;
  readonly icon: string;
  readonly accepted: boolean;
  readonly acceptedAtUtc: TimestampIso | null;
  readonly ipAddress: string | null;
}

/** GetAvailabilityQuery.cs SlotDto. */
export interface SlotDto {
  readonly time: TimeStr;
  readonly available: boolean;
  readonly seatsLeft: number;
}

/** PublicSiteEndpoints.cs PublicBookingRequest (guest booking). */
export interface PublicBookingRequest {
  readonly fullName: string;
  readonly phone: string;
  readonly email?: string | null;
  readonly date: DateStr;
  readonly time: TimeStr;
  readonly boatClass?: string | null;
  readonly note?: string | null;
  readonly reminderMinutes?: number | null;
  readonly acceptedConsents?: readonly string[] | null;
}

/** BookAppointmentCommand.cs BookAppointmentResponse — shared by guest & member booking. */
export interface BookAppointmentResponse {
  readonly appointmentId: string;
  readonly sessionId: string;
  readonly date: DateStr;
  readonly startTime: TimeStr;
  readonly boatClass: BoatClassLabel;
  readonly level: number;
  readonly boatName: string | null;
  readonly instructorName: string | null;
  readonly reminderMinutes: number | null;
  readonly status: string;
}

/** MemberEndpoints.cs MemberRegisterRequest / MemberLoginRequest. */
export interface MemberRegisterRequest {
  readonly fullName: string;
  readonly phone: string;
  readonly email: string;
  readonly password: string;
  readonly acceptedConsents?: readonly string[] | null;
}

export interface MemberLoginRequest {
  readonly email: string;
  readonly password: string;
}
