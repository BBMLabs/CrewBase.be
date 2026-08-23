export const API_BASE_URL: string =
  (import.meta.env["VITE_API_BASE_URL"] as string | undefined) ?? "http://localhost:5283";

/**
 * Club subdomain resolution for dev: browsers resolve *.localhost to 127.0.0.1, but running
 * the portal at localhost:5174 has no subdomain — `VITE_CLUB_SUBDOMAIN` (or ?club=) fills it.
 * In production (*.faturebase.com) the host itself carries the subdomain.
 */
export const DEV_CLUB_SUBDOMAIN: string | undefined =
  (import.meta.env["VITE_CLUB_SUBDOMAIN"] as string | undefined) ?? undefined;
