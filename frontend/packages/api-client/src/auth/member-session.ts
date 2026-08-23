/**
 * Member authentication strategy (tenant Customer identity space).
 * VERIFIED backend behaviour: members receive a SHORT-LIVED ACCESS TOKEN ONLY.
 * There is NO refresh endpoint — expiry ⇒ re-login is required. The issue response
 * carries `expiresAtUtc`, which drives the proactive session-expired UX.
 */
import type {
  MemberAuthResponse,
  MemberLoginRequest,
  MemberRegisterRequest,
} from "@crewbase/api-types";
import type { ApiClient } from "../http/api-client";
import { memberSessionStorage, type TokenStorage } from "./storage";

export interface MemberStoredSession {
  readonly member: MemberAuthResponse["member"];
  readonly accessToken: string;
  readonly expiresAtUtc: string;
}

const EXPIRY_SAFETY_MS = 30_000;

export class MemberSession {
  private expiredListeners: Array<() => void> = [];

  constructor(
    private readonly client: ApiClient,
    private readonly storage: TokenStorage = memberSessionStorage(),
  ) {}

  current(): MemberStoredSession | null {
    const raw = this.storage.read();
    if (!raw) return null;
    try {
      const parsed: unknown = JSON.parse(raw);
      return isStoredSession(parsed) ? parsed : null;
    } catch {
      return null;
    }
  }

  getAccessToken(): string | null {
    const session = this.current();
    if (!session) return null;
    if (this.isExpired(session)) {
      this.clear();
      this.emitExpired();
      return null;
    }
    return session.accessToken;
  }

  isExpired(session: MemberStoredSession | null = this.current()): boolean {
    if (!session) return false;
    return new Date(session.expiresAtUtc).getTime() - EXPIRY_SAFETY_MS <= Date.now();
  }

  msUntilExpiry(): number {
    const session = this.current();
    if (!session) return 0;
    return Math.max(0, new Date(session.expiresAtUtc).getTime() - Date.now());
  }

  /** Auth hooks never attempt refresh (no such endpoint exists) — always false. */
  onUnauthorized(): Promise<boolean> {
    this.clear();
    this.emitExpired();
    return Promise.resolve(false);
  }

  async login(subdomain: string, request: MemberLoginRequest): Promise<MemberAuthResponse> {
    const res = await this.client.post<MemberLoginRequest, MemberAuthResponse>(
      `/api/v1/public/${encodeURIComponent(subdomain)}/members/login`,
      request,
      { anonymous: true },
    );
    this.persist(res);
    return res;
  }

  async register(subdomain: string, request: MemberRegisterRequest): Promise<MemberAuthResponse> {
    const res = await this.client.post<MemberRegisterRequest, MemberAuthResponse>(
      `/api/v1/public/${encodeURIComponent(subdomain)}/members/register`,
      request,
      { anonymous: true },
    );
    this.persist(res);
    return res;
  }

  clear(): void {
    this.storage.clear();
  }

  onExpired(listener: () => void): () => void {
    this.expiredListeners.push(listener);
    return () => {
      this.expiredListeners = this.expiredListeners.filter((l) => l !== listener);
    };
  }

  private emitExpired(): void {
    for (const listener of this.expiredListeners) listener();
  }

  private persist(response: MemberAuthResponse): void {
    const session: MemberStoredSession = {
      member: response.member,
      accessToken: response.accessToken,
      expiresAtUtc: response.expiresAtUtc,
    };
    this.storage.write(JSON.stringify(session));
  }
}

function isStoredSession(value: unknown): value is MemberStoredSession {
  if (typeof value !== "object" || value === null) return false;
  const record = value as Record<string, unknown>;
  return (
    typeof record["accessToken"] === "string" &&
    typeof record["expiresAtUtc"] === "string" &&
    typeof record["member"] === "object" &&
    record["member"] !== null
  );
}
