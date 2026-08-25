import { create } from "zustand";
import type { CompanySessionSnapshot } from "@crewbase/api-client";

export type SessionStatus = "unknown" | "authenticated" | "guest";

interface SessionState {
  readonly status: SessionStatus;
  readonly email: string | null;
  readonly role: string | null;
  readonly companyId: string | null;
  /** Applies a fresh snapshot read from the session kernel. */
  applySnapshot: (snapshot: CompanySessionSnapshot) => void;
  markGuest: () => void;
  clear: () => void;
}

/**
 * Client-only session metadata (never tokens — those live in the storage adapters
 * owned by the api-client session kernel).
 */
export const useSessionStore = create<SessionState>()((set) => ({
  status: "unknown",
  email: null,
  role: null,
  companyId: null,
  applySnapshot: (snapshot) =>
    set({
      status: snapshot.userId !== null ? "authenticated" : "guest",
      email: snapshot.email,
      role: snapshot.role,
      companyId: snapshot.companyId,
    }),
  markGuest: () => set({ status: "guest", email: null, role: null, companyId: null }),
  clear: () => set({ status: "guest", email: null, role: null, companyId: null }),
}));
