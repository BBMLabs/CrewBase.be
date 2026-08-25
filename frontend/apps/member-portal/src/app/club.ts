import { currentSubdomain } from "@crewbase/api-client";
import { DEV_CLUB_SUBDOMAIN } from "./env";
import { useSessionStore } from "../features/auth/session-store";

const CLUB_STORAGE_KEY = "crewbase.club";

/** Dev-persisted club choice shared with the public site (backend still validates it). */
export function readPersistedClub(): string | null {
  try {
    return globalThis.localStorage?.getItem(CLUB_STORAGE_KEY) ?? null;
  } catch {
    return null;
  }
}

export function persistClub(subdomain: string): void {
  try {
    globalThis.localStorage?.setItem(CLUB_STORAGE_KEY, subdomain);
  } catch {
    /* ignore */
  }
}

/**
 * Club context resolution order:
 *   1. host subdomain (*.localhost dev / *.faturebase.com prod)
 *   2. ?club= query override
 *   3. subdomain persisted at login time — member API is claim-scoped, but public
 *      options/availability/consents still need the club segment
 *   4. dev-persisted choice (localStorage, shared with public site)
 *   5. VITE_CLUB_SUBDOMAIN env fallback
 */
export function resolveClubSubdomain(): string | null {
  const fromHost = currentSubdomain();
  if (fromHost !== null) return fromHost;

  const fromQuery = new URLSearchParams(globalThis.location.search).get("club");
  if (fromQuery !== null && fromQuery !== "") return fromQuery;

  const stored = useSessionStore.getState().subdomain;
  if (stored !== null && stored !== "") return stored;

  const persisted = readPersistedClub();
  if (persisted !== null && persisted !== "") return persisted;

  return DEV_CLUB_SUBDOMAIN ?? null;
}
