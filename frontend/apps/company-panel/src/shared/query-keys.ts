/** Query key factory — tenant scope (companyId claim) is implicit per app instance but
 *  included so a future multi-account browser session never crosses caches. */
import type { AppointmentStatus } from "@crewbase/api-types";

export const panelKeys = {
  all: (companyId: string | null) => ["panel", companyId] as const,

  stats: (companyId: string | null) => [...panelKeys.all(companyId), "stats"] as const,
  site: (companyId: string | null) => [...panelKeys.all(companyId), "site"] as const,

  appointments: {
    root: (companyId: string | null) => [...panelKeys.all(companyId), "appointments"] as const,
    list: (companyId: string | null, date: string | null) =>
      [...panelKeys.appointments.root(companyId), "list", { date }] as const,
  },

  sessions: {
    root: (companyId: string | null) => [...panelKeys.all(companyId), "sessions"] as const,
    day: (companyId: string | null, date: string) =>
      [...panelKeys.sessions.root(companyId), "day", { date }] as const,
  },

  members: {
    root: (companyId: string | null) => [...panelKeys.all(companyId), "members"] as const,
    list: (companyId: string | null) => [...panelKeys.members.root(companyId), "list"] as const,
    packages: (companyId: string | null, customerId: string) =>
      [...panelKeys.members.root(companyId), "packages", customerId] as const,
    logs: (companyId: string | null, customerId: string, take: number) =>
      [...panelKeys.members.root(companyId), "logs", customerId, { take }] as const,
  },

  recentLogs: (companyId: string | null, take: number) =>
    [...panelKeys.all(companyId), "logs", { take }] as const,

  packageBalances: (companyId: string | null) => [...panelKeys.all(companyId), "package-balances"] as const,

  boats: (companyId: string | null) => [...panelKeys.all(companyId), "boats"] as const,
  instructors: (companyId: string | null) => [...panelKeys.all(companyId), "instructors"] as const,
  packages: (companyId: string | null) => [...panelKeys.all(companyId), "packages"] as const,

  settings: (companyId: string | null) => [...panelKeys.all(companyId), "settings"] as const,
  closedDates: (companyId: string | null) => [...panelKeys.all(companyId), "closed-dates"] as const,

  feed: {
    root: (companyId: string | null) => [...panelKeys.all(companyId), "feed"] as const,
    list: (companyId: string | null) => [...panelKeys.feed.root(companyId), "list"] as const,
    media: (companyId: string | null, postId: string) =>
      [...panelKeys.feed.root(companyId), "media", postId] as const,
    comments: (companyId: string | null, postId: string) =>
      [...panelKeys.feed.root(companyId), "comments", postId] as const,
    participants: (companyId: string | null, postId: string) =>
      [...panelKeys.feed.root(companyId), "participants", postId] as const,
  },

  users: (companyId: string | null) => [...panelKeys.all(companyId), "users"] as const,
};

export type { AppointmentStatus };
