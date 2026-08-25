/** Runtime configuration from Vite env (no secrets — browser bundle is public by design). */
export const API_BASE_URL: string =
  (import.meta.env["VITE_API_BASE_URL"] as string | undefined) ?? "http://localhost:5283";

export interface AppEnv {
  readonly apiBaseUrl: string;
}

export function readEnv(): AppEnv {
  return { apiBaseUrl: API_BASE_URL };
}
