/**
 * Token storage adapters.
 * Tradeoff documented in FRONTEND-ARCHITECTURE.md §5: the backend is header-Bearer only
 * (no cookies), so tokens must live in Web Storage. Company refresh token → localStorage
 * (session continuity), member short-lived access token → sessionStorage (least surprise).
 */

export interface TokenStorage {
  read(): string | null;
  write(token: string): void;
  clear(): void;
}

function createStorage(area: "localStorage" | "sessionStorage", key: string): TokenStorage {
  return {
    read() {
      try {
        return area === "localStorage"
          ? globalThis.localStorage.getItem(key)
          : globalThis.sessionStorage.getItem(key);
      } catch {
        return null;
      }
    },
    write(token: string) {
      try {
        if (area === "localStorage") globalThis.localStorage.setItem(key, token);
        else globalThis.sessionStorage.setItem(key, token);
      } catch {
        /* storage unavailable (private mode) — session lives in memory only */
      }
    },
    clear() {
      try {
        if (area === "localStorage") globalThis.localStorage.removeItem(key);
        else globalThis.sessionStorage.removeItem(key);
      } catch {
        /* ignore */
      }
    },
  };
}

/** Persistent across tabs/restarts — company user access+refresh pair. */
export const companyAccessStorage = (): TokenStorage =>
  createStorage("localStorage", "crewbase.company.accessToken");
export const companyRefreshStorage = (): TokenStorage =>
  createStorage("localStorage", "crewbase.company.refreshToken");
export const companyExpiryStorage = (): TokenStorage =>
  createStorage("localStorage", "crewbase.company.expiresAtUtc");

/** Tab-scoped, short-lived — member portal has no refresh flow. */
export const memberSessionStorage = (): TokenStorage =>
  createStorage("sessionStorage", "crewbase.member.session");
