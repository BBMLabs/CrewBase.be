/**
 * Central Turkish labels for backend domain values.
 * Components must never inline these — future i18n swaps this module only.
 * Sources: AppointmentStatus (Appointment.cs), BoatClass.Label() (Boat.cs),
 * RowingLevels.Labels (RowingLevels.cs), MemberEvents (MemberCommon.cs) + handler extras,
 * MembershipCard enums, Friendship direction/status strings, PostMediaKind.
 */
import { ROWING_LEVEL_LABELS, type BoatClassLabel } from "@crewbase/api-types";

export const APPOINTMENT_STATUS_LABELS: Readonly<Record<string, string>> = {
  Pending: "Onay Bekliyor",
  Confirmed: "Onaylandı",
  Completed: "Tamamlandı",
  Cancelled: "İptal Edildi",
};

export const BOAT_CLASS_LABELS: Readonly<Record<BoatClassLabel, string>> = {
  "1x": "Tek kürek (1x)",
  "2x": "Çift kürek (2x)",
  "4x": "Dört kürek (4x)",
};

export const ROWING_LEVEL_LABEL_LIST = ROWING_LEVEL_LABELS;

export function rowingLevelLabel(level: number): string {
  return ROWING_LEVEL_LABELS[level] ?? String(level);
}

export const MEMBER_LOG_EVENT_LABELS: Readonly<Record<string, string>> = {
  MEMBER_REGISTERED: "Üye kaydı",
  MEMBER_LOGIN: "Giriş",
  PROFILE_UPDATED: "Profil güncellendi",
  ACCOUNT_DELETED: "Hesap silindi",
  APPOINTMENT_BOOKED: "Randevu alındı",
  APPOINTMENT_CANCELLED: "Randevu iptal edildi",
  PACKAGE_ASSIGNED: "Paket tanımlandı",
  PACKAGE_DEDUCTED: "Paket dersi düşüldü",
  PACKAGE_REFUNDED: "Paket dersi iade edildi",
  LEVEL_CHANGED: "Seviye değişti",
  CONSENTS_SUBMITTED: "Beyanlar kaydedildi",
  FRIEND_REQUEST_SENT: "Arkadaşlık isteği gönderildi",
  CARD_UPDATED: "Üyelik kartı güncellendi",
}

export const MEMBER_LOG_EVENT_UNLABELED = "Diğer olay";

export const CARD_TYPE_LABELS: Readonly<Record<string, string>> = {
  Multisport: "Multisport",
  Meditopia: "Meditopia",
};

export const CARD_STATUS_LABELS: Readonly<Record<string, string>> = {
  Active: "Aktif",
  Passive: "Pasif",
  Expired: "Süresi Geçmiş",
};

export const FRIEND_DIRECTION_LABELS: Readonly<Record<string, string>> = {
  incoming: "Gelen istek",
  outgoing: "Gönderilen istek",
  friend: "Arkadaş",
};

export const MEDIA_KIND_LABELS: Readonly<Record<string, string>> = {
  None: "",
  Image: "Görsel",
  Video: "Video",
};

export const USER_ROLE_LABELS: Readonly<Record<string, string>> = {
  PlatformAdmin: "Platform Yöneticisi",
  CompanyAdmin: "Firma Yöneticisi",
  Employee: "Çalışan",
  Member: "Üye",
}

export const COMPANY_STATUS_LABELS: Readonly<Record<string, string>> = {
  PendingApproval: "Onay Bekliyor",
  Active: "Aktif",
  Suspended: "Askıda",
}

/** Weekday names indexed by settings openDays convention (0=Sunday … 6=Saturday). */
export const WEEKDAY_LABELS: readonly string[] = [
  "Pazar",
  "Pazartesi",
  "Salı",
  "Çarşamba",
  "Perşembe",
  "Cuma",
  "Cumartesi",
];

export const WEEKDAY_SHORT_LABELS: readonly string[] = [
  "Paz",
  "Pzt",
  "Sal",
  "Çar",
  "Per",
  "Cum",
  "Cmt",
];
