/**
 * Domain enums as serialized on the wire (strings, not numbers).
 * Sources cited per union; verified 2026-08.
 */

/** Appointment.cs enum AppointmentStatus → `a.Status.ToString()` in DTOs. */
export type AppointmentStatus = "Pending" | "Confirmed" | "Cancelled" | "Completed";

/** Boat.cs Label(): Single1x→"1x", Double2x→"2x", Quad4x→"4x". Capacity: 1/2/4. */
export type BoatClassLabel = "1x" | "2x" | "4x";
export const BOAT_CLASS_CAPACITY: Readonly<Record<BoatClassLabel, number>> = {
  "1x": 1,
  "2x": 2,
  "4x": 4,
};

/** User.cs enum UserRole. */
export type CompanySideRole = "PlatformAdmin" | "CompanyAdmin" | "Employee";
/** Member role is a separate identity space (MemberTokenIssuer.cs). */
export type MemberSideRole = "Member";
export type AnyRole = CompanySideRole | MemberSideRole;

/** User.cs enum UserStatus. */
export type UserStatus = "Active" | "Deactivated";

/** Company.cs enum CompanyStatus. */
export type CompanyStatus = "PendingApproval" | "Active" | "Suspended";

/** ConsentCatalog.cs ConsentScope.ToString(). */
export type ConsentScope = "Booking" | "Member";

/** MembershipCard.cs enums. */
export type MembershipCardType = "Multisport" | "Meditopia";
export type MembershipCardStatus = "Active" | "Passive" | "Expired";

/** Post.cs enum PostMediaKind.ToString(). */
export type PostMediaKind = "None" | "Image" | "Video";

/** SocialCommands.cs GetFriendsQueryHandler direction strings. */
export type FriendDirection = "incoming" | "outgoing" | "friend";
/** FriendshipStatus.ToString(); Rejected rows are filtered server-side. */
export type FriendshipStatus = "Pending" | "Accepted" | "Rejected";

/** OtpCommands purpose values (backend compares raw strings). */
export type OtpPurpose = "email" | "phone";

/** RowingLevels.Labels — exact Turkish labels, index = level (0..10). */
export const ROWING_LEVEL_LABELS: readonly string[] = [
  "Hiç çekmedim",
  "Temel eğitimim yeni başladı (1-2. ders)",
  "Temel eğitimim sürüyor (3+ ders)",
  "Temel eğitim tamamlandı",
  "1+ yıldır çekiyorum",
  "Bölgesel yarıştım",
  "Ulusal yarıştım",
  "Ulusal madalya kazandım",
  "Uluslararası yarıştım",
  "Milli takımda yer aldım",
  "Milli takımda madalya kazandım",
];

/** MemberEvents + extra log event constants seen in handlers. */
export const MEMBER_LOG_EVENTS = [
  "MEMBER_REGISTERED",
  "MEMBER_LOGIN",
  "PROFILE_UPDATED",
  "ACCOUNT_DELETED",
  "APPOINTMENT_BOOKED",
  "APPOINTMENT_CANCELLED",
  "PACKAGE_ASSIGNED",
  "PACKAGE_DEDUCTED",
  "PACKAGE_REFUNDED",
  "LEVEL_CHANGED",
  "CONSENTS_SUBMITTED",
  "FRIEND_REQUEST_SENT",
  "CARD_UPDATED",
] as const;
export type MemberLogEvent = (typeof MEMBER_LOG_EVENTS)[number];
