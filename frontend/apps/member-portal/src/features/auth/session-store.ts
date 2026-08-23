import { create } from "zustand";
import type { MemberStoredSession } from "@crewbase/api-client";

export type SessionStatus = "unknown" | "authenticated" | "guest" | "expired";

interface SessionState {
  readonly status: SessionStatus;
  readonly member: MemberStoredSession["member"] | null;
  readonly clubName: string;
  readonly subdomain: string | null;
  setClub: (subdomain: string, name: string) => void;
  applySession: (session: MemberStoredSession) => void;
  markExpired: () => void;
  markGuest: () => void;
  clear: () => void;
}

export const useSessionStore = create<SessionState>()((set) => ({
  status: "unknown",
  member: null,
  clubName: "",
  subdomain: null,
  setClub: (subdomain, name) => set({ subdomain, clubName: name }),
  applySession: (session) =>
    set({ status: "authenticated", member: session.member }),
  markExpired: () =>
    set((state) =>
      state.status === "authenticated" ? { status: "expired", member: null } : { status: state.status },
    ),
  markGuest: () => set({ status: "guest", member: null }),
  clear: () => set({ status: "guest", member: null }),
}));

/** Server-state cache lives in TanStack Query — this store is session metadata only. */
