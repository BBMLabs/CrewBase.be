/**
 * Domain-agnostic HTTP core. One instance per app, bound with auth hooks at bootstrap.
 * Retry policy: none (mutations are non-idempotent; auth endpoints rate-limited 10/min/IP).
 */
import type { ApiEnvelope } from "@crewbase/api-types";
import {
  cancelledError,
  emptyStatus,
  errorFromResponse,
  networkError,
  timeoutError,
} from "./app-error";
import type { AppError } from "./app-error";

export interface AuthHooks {
  /** Current bearer token or null. */
  readonly getAccessToken: () => string | null;
  /**
   * Called once on 401. Return true to replay the original request exactly once
   * (company strategy: single-flight refresh). Member/public return false.
   */
  readonly onUnauthorized: (error: AppError) => Promise<boolean>;
}

export interface ApiClientConfig {
  readonly baseUrl: string;
  readonly authHooks?: AuthHooks;
  /** Per-request timeout in ms. Default 15000. */
  readonly timeoutMs?: number;
  /** Stable correlation id for the app session; sent as X-Correlation-ID. */
  readonly correlationId?: string;
  readonly fetchImpl?: typeof fetch;
}

export interface RequestOptions {
  readonly query?: Readonly<Record<string, string | number | boolean | null | undefined>>;
  readonly body?: unknown;
  readonly signal?: AbortSignal;
  /** Skip Authorization header (public endpoints). Default: attach if a token exists. */
  readonly anonymous?: boolean;
}

export class ApiClient {
  private readonly baseUrl: string;
  private readonly authHooks: AuthHooks | undefined;
  private readonly timeoutMs: number;
  private readonly correlationId: string | undefined;
  private readonly fetchImpl: typeof fetch;

  constructor(config: ApiClientConfig) {
    this.baseUrl = config.baseUrl.replace(/\/+$/, "");
    this.authHooks = config.authHooks;
    this.timeoutMs = config.timeoutMs ?? 15_000;
    this.correlationId = config.correlationId;
    this.fetchImpl = config.fetchImpl ?? globalThis.fetch.bind(globalThis);
  }

  get<TResponse>(path: string, options?: RequestOptions): Promise<TResponse> {
    return this.request<TResponse>("GET", path, options);
  }

  post<TRequest, TResponse = void>(
    path: string,
    body?: TRequest,
    options?: Omit<RequestOptions, "body">,
  ): Promise<TResponse> {
    return this.request<TResponse>("POST", path, { ...options, body });
  }

  put<TRequest, TResponse = void>(
    path: string,
    body?: TRequest,
    options?: Omit<RequestOptions, "body">,
  ): Promise<TResponse> {
    return this.request<TResponse>("PUT", path, { ...options, body });
  }

  delete<TResponse = void>(path: string, options?: RequestOptions): Promise<TResponse> {
    return this.request<TResponse>("DELETE", path, options);
  }

  async request<TResponse>(
    method: "GET" | "POST" | "PUT" | "DELETE",
    path: string,
    options: RequestOptions = {},
  ): Promise<TResponse> {
    const res = await this.send(method, path, options);

    let payload: unknown = null;
    if (!emptyStatus(res.status)) {
      try {
        payload = await res.json();
      } catch {
        // Non-JSON error bodies (e.g. plain-text 404 from /site/{sub}) → generic handling below.
      }
    }

    if (res.ok) {
      if (
        payload !== null &&
        typeof payload === "object" &&
        "success" in payload &&
        (payload as { success: unknown }).success === true &&
        "data" in payload
      ) {
        return (payload as ApiEnvelope<TResponse>).data as TResponse;
      }
      // Some success responses may be raw (defensive); surface as-is.
      return payload as TResponse;
    }

    const error = errorFromResponse(
      res,
      payload,
      "İstek başarısız oldu.",
      undefined,
    );

    if (res.status === 401 && !options.anonymous && this.authHooks) {
      const retried = await this.authHooks.onUnauthorized(error).catch(() => false);
      if (retried) {
        const retryRes = await this.send(method, path, options);
        let retryPayload: unknown = null;
        if (!emptyStatus(retryRes.status)) {
          try {
            retryPayload = await retryRes.json();
          } catch {
            /* as above */
          }
        }
        if (retryRes.ok) {
          if (
            retryPayload !== null &&
            typeof retryPayload === "object" &&
            "success" in retryPayload &&
            "data" in retryPayload
          ) {
            return (retryPayload as ApiEnvelope<TResponse>).data as TResponse;
          }
          return retryPayload as TResponse;
        }
        throw errorFromResponse(retryRes, retryPayload, "İstek başarısız oldu.");
      }
    }

    throw error;
  }

  private async send(
    method: "GET" | "POST" | "PUT" | "DELETE",
    path: string,
    options: RequestOptions,
  ): Promise<Response> {
    const url = new URL(`${this.baseUrl}${path}`, this.baseUrl);
    if (options.query) {
      for (const [key, value] of Object.entries(options.query)) {
        if (value === undefined || value === null || value === "") continue;
        url.searchParams.set(key, String(value));
      }
    }

    const headers = new Headers({ Accept: "application/json" });
    if (!options.anonymous) {
      const token = this.authHooks?.getAccessToken();
      if (token) headers.set("Authorization", `Bearer ${token}`);
    }
    if (options.body !== undefined) headers.set("Content-Type", "application/json");
    if (this.correlationId) headers.set("X-Correlation-ID", this.correlationId);

    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort("timeout"), this.timeoutMs);
    const onOuterAbort = () => controller.abort("cancelled");
    options.signal?.addEventListener("abort", onOuterAbort, { once: true });

    try {
      return await this.fetchImpl(url.toString(), {
        method,
        headers,
        credentials: "include",
        body:
          options.body === undefined
            ? undefined
            : options.body instanceof FormData
              ? options.body
              : JSON.stringify(options.body),
        signal: controller.signal,
      });
    } catch (cause) {
      if (controller.signal.aborted) {
        if (options.signal?.aborted) throw cancelledError();
        const reason = (controller.signal.reason as string | undefined) ?? "";
        if (reason === "timeout") throw timeoutError();
        throw cancelledError();
      }
      throw networkError(cause);
    } finally {
      clearTimeout(timer);
      options.signal?.removeEventListener("abort", onOuterAbort);
    }
  }
}

/** Creates a correlation id for the app session (crypto.randomUUID when available). */
export function createCorrelationId(): string {
  return globalThis.crypto?.randomUUID?.() ?? `cb-${Date.now()}-${Math.random().toString(36).slice(2)}`;
}
