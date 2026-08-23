/**
 * Company-user authentication strategy (PlatformAdmin/CompanyAdmin/Employee identity space).
 * Access token (15 min) + rotating refresh token with server-side reuse detection.
 * On 401: single-flight POST /auth/refresh, replay original request once.
 * Refresh reuse or failure ⇒ whole family is revoked server-side → hard local logout.
 */
import type { LoginResult, RefreshTokenRequest, RefreshTokenResponse } from "@crewbase/api-types";
import type { ApiClient } from "../http/api-client";
import type { AppError } from "../http/app-error";
import { decodeJwtPayload } from "./jwt";
import {
  companyAccessStorage,
  companyExpiryStorage,
  companyRefreshStorage,
  type TokenStorage,
} from "./storage";

export interface CompanySessionSnapshot {
  readonly userId: string | null;
  readonly email: string | null;
  readonly role: string | null;
  readonly companyId: string | null;
  readonly expiresAtUtcIso: string | null;
}

export class CompanyUserSession {
  private refreshInFlight: Promise<boolean> | null = null;

  constructor(
    private readonly client: ApiClient,
    private readonly accessStorage: TokenStorage = companyAccessStorage(),
    private readonly refreshStorage: TokenStorage = companyRefreshStorage(),
    private readonly expiryStorage: TokenStorage = companyExpiryStorage(),
  ) {}

  getAccessToken(): string | null {
    return this.accessStorage.read();
  }

  snapshot(): CompanySessionSnapshot {
    const token = this.accessStorage.read();
    if (!token) {
      return {
        userId: null,
        email: null,
        role: null,
        companyId: null,
        expiresAtUtcIso: this.expiryStorage.read(),
      };
    }
    const payload = decodeJwtPayload(token);
    return {
      userId: payload?.sub ?? null,
      email: payload?.email ?? null,
      role: payload?.role ?? null,
      companyId: payload?.companyId ?? null,
      expiresAtUtcIso: this.expiryStorage.read(),
    };
  }

  /** Milliseconds until proactive-refresh moment (expiry − 60s), clamped ≥ 0. */
  msUntilRefreshDue(): number {
    const raw = this.expiryStorage.read();
    if (!raw) return 0;
    const due = new Date(raw).getTime() - 60_000 - Date.now();
    return Math.max(0, Number.isNaN(due) ? 0 : due);
  }

  hasRefreshToken(): boolean {
    return this.refreshStorage.read() !== null;
  }

  async login(email: string, password: string): Promise<LoginResult> {
    const result = await this.client.post<unknown, LoginResult>(
      "/api/v1/auth/login",
      { email, password },
      { anonymous: true },
    );
    if (result.accessToken && result.refreshToken && result.accessTokenExpiresAtUtc) {
      this.persist(result.accessToken, result.refreshToken, result.accessTokenExpiresAtUtc);
    }
    return result;
  }

  /**
   * Single-flight refresh. Returns true when a fresh access token is available.
   * Any failure clears local state (covers reuse-detection family revocation).
   */
  refreshNow(): Promise<boolean> {
    this.refreshInFlight ??= this.doRefresh().finally(() => {
      this.refreshInFlight = null;
    });
    return this.refreshInFlight;
  }

  onUnauthorized(_error: AppError): Promise<boolean> {
    return this.refreshNow();
  }

  /** Idempotent server-side logout of the current refresh family + local cleanup. */
  async logout(): Promise<void> {
    const refreshToken = this.refreshStorage.read();
    if (refreshToken) {
      await this.client
        .post("/api/v1/auth/logout", { refreshToken }, { anonymous: true })
        .catch(() => undefined);
    }
    this.clearLocal();
  }

  /** Requires valid access token; invalidates every session of the user. */
  async logoutAll(): Promise<void> {
    await this.client.post("/api/v1/auth/logout-all", undefined).catch(() => undefined);
    this.clearLocal();
  }

  clearLocal(): void {
    this.accessStorage.clear();
    this.refreshStorage.clear();
    this.expiryStorage.clear();
  }

  private async doRefresh(): Promise<boolean> {
    const refreshToken = this.refreshStorage.read();
    if (!refreshToken) return false;
    try {
      const res = await this.client.post<RefreshTokenRequest, RefreshTokenResponse>(
        "/api/v1/auth/refresh",
        { refreshToken },
        { anonymous: true },
      );
      this.persist(res.accessToken, res.refreshToken, res.accessTokenExpiresAtUtc);
      return true;
    } catch {
      this.clearLocal();
      return false;
    }
  }

  private persist(accessToken: string, refreshToken: string, expiresAtUtcIso: string): void {
    this.accessStorage.write(accessToken);
    this.refreshStorage.write(refreshToken);
    this.expiryStorage.write(expiresAtUtcIso);
  }
}
