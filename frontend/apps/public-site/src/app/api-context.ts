import { ApiClient, createPublicService, createCorrelationId, type PublicService } from "@crewbase/api-client";

export const API_BASE_URL: string =
  (import.meta.env["VITE_API_BASE_URL"] as string | undefined) ?? "http://localhost:5283";

export function subdomainFromHostLocal(host: string): string | null {
  const hostName = host.split(":")[0]?.toLowerCase() ?? "";
  const BASE = "faturebase.com";
  let candidate: string | null = null;
  if (hostName.endsWith(`.${BASE}`)) candidate = hostName.slice(0, -(BASE.length + 1));
  else if (hostName.endsWith(".localhost")) candidate = hostName.slice(0, -".localhost".length);
  if (candidate === null || candidate === "" || candidate.includes(".") || candidate === "www") return null;
  return candidate;
}

const CLUB_STORAGE_KEY = "crewbase.club";

/** Dev-persisted club choice (user-supplied context; backend still validates it). */
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

export function resolveSubdomain(): string | null {
  const fromHost = subdomainFromHostLocal(globalThis.location.hostname);
  if (fromHost !== null) return fromHost;
  const fromQuery = new URLSearchParams(globalThis.location.search).get("club");
  if (fromQuery !== null && fromQuery !== "") return fromQuery;
  return readPersistedClub();
}

const publicService: PublicService = createPublicService(
  new ApiClient({ baseUrl: API_BASE_URL, correlationId: createCorrelationId() }),
);

export function usePublicApi(): PublicService {
  return publicService;
}
