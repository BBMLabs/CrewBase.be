/**
 * SignalR chat connection manager — the ONLY place that touches @microsoft/signalr.
 *
 * Verified backend behaviour (ChatHub.cs + AuthenticationSetup.cs + CORS):
 * - hub at /hubs/chat, Member-role authorized
 * - websockets cannot carry headers → token via `?access_token=` query
 * - incoming event: "message" with MessageDto payload
 * - server user key = `{company_id}:{sub}` (irrelevant to client)
 *
 * Lifecycle rules:
 * - lazy start on first acquire (ref-counted; StrictMode double-mount safe)
 * - automatic reconnect w/ fallback manual resync callback
 * - stop + teardown on logout / session expiry / user change
 * - access token is never logged or included in error surfaces
 */
import { HubConnectionBuilder, HttpTransportType, LogLevel } from "@microsoft/signalr";
import type { HubConnection } from "@microsoft/signalr";
import type { MessageDto } from "@crewbase/api-types";

export type ChatStatus = "idle" | "connecting" | "connected" | "reconnecting" | "stopped";

export interface ChatManagerOptions {
  readonly apiBaseUrl: string;
  readonly getAccessToken: () => string | null;
  /** Called for every incoming "message" event. */
  readonly onMessage: (message: MessageDto) => void;
  /** Called after a reconnect so callers can REST-resync missed messages. */
  readonly onReconnect?: () => void;
  /** Called when the connection fails in a way that suggests auth problems (401/negotiate). */
  readonly onFatal?: () => void;
}

class ChatConnectionManager {
  private connection: HubConnection | null = null;
  private refCount = 0;
  private status: ChatStatus = "idle";
  private statusListeners = new Set<(status: ChatStatus) => void>();
  private options: ChatManagerOptions | null = null;

  configure(options: ChatManagerOptions): void {
    this.options = options;
  }

  getStatus(): ChatStatus {
    return this.status;
  }

  onStatus(listener: (status: ChatStatus) => void): () => void {
    this.statusListeners.add(listener);
    listener(this.status);
    return () => {
      this.statusListeners.delete(listener);
    };
  }

  private setStatus(status: ChatStatus): void {
    if (this.status === status) return;
    this.status = status;
    for (const listener of this.statusListeners) listener(status);
  }

  async acquire(): Promise<void> {
    this.refCount += 1;
    if (this.connection !== null && this.refCount === 1) {
      await this.start().catch(() => undefined);
      return;
    }
    if (this.connection !== null) return;
    this.build();
    await this.start().catch(() => undefined);
  }

  release(): void {
    this.refCount = Math.max(0, this.refCount - 1);
    if (this.refCount === 0) {
      void this.stop();
    }
  }

  /** Hard reset (logout / user change). Stops and clears regardless of ref-count. */
  async reset(): Promise<void> {
    this.refCount = 0;
    await this.stop();
    this.options = null;
  }

  private build(): void {
    const options = this.options;
    if (options === null) throw new Error("ChatConnectionManager yapılandırılmadı");

    const url = `${options.apiBaseUrl.replace(/\/+$/, "")}/hubs/chat`;
    this.connection = new HubConnectionBuilder()
      .withUrl(url, { transport: HttpTransportType.WebSockets })
      .withAutomaticReconnect([0, 2000, 5000, 10_000])
      .configureLogging(LogLevel.Error)
      .build();

    // Token is attached per-start below via the factory; never logged.
    this.connection.on("message", (payload: unknown) => {
      const message = asMessage(payload);
      if (message !== null) options.onMessage(message);
    });

    this.connection.onreconnecting(() => this.setStatus("reconnecting"));
    this.connection.onreconnected(() => {
      this.setStatus("connected");
      options.onReconnect?.();
    });
    this.connection.onclose(() => {
      this.setStatus("stopped");
      // If we still have consumers, attempt one rebuild+start cycle (auth may have refreshed).
      if (this.refCount > 0 && this.connection !== null) {
        setTimeout(() => {
          if (this.refCount > 0) void this.start().catch(() => options.onFatal?.());
        }, 1500);
      }
    });
  }

  private async start(): Promise<void> {
    const connection = this.connection;
    const options = this.options;
    if (connection === null || options === null) return;

    const token = options.getAccessToken();
    if (token === null) {
      options.onFatal?.();
      return;
    }

    this.setStatus(this.status === "connected" ? "connected" : "connecting");
    try {
      // Re-create builder input with fresh token each (re)start:
      connection.baseUrl = `${options.apiBaseUrl.replace(/\/+$/, "")}/hubs/chat?access_token=${encodeURIComponent(token)}`;
      await connection.start();
      this.setStatus("connected");
    } catch {
      this.setStatus("stopped");
      // Distinguish auth failure from transient network: negotiate failures surface here.
      options.onFatal?.();
    }
  }

  private async stop(): Promise<void> {
    const connection = this.connection;
    this.connection = null;
    this.setStatus("idle");
    if (connection !== null) {
      try {
        await connection.stop();
      } catch {
        /* already stopped */
      }
    }
  }
}

function asMessage(payload: unknown): MessageDto | null {
  if (typeof payload !== "object" || payload === null) return null;
  const record = payload as Record<string, unknown>;
  if (
    typeof record["id"] !== "string" ||
    typeof record["senderId"] !== "string" ||
    typeof record["recipientId"] !== "string" ||
    typeof record["body"] !== "string" ||
    typeof record["sentAtUtc"] !== "string"
  ) {
    return null;
  }
  return {
    id: record["id"],
    senderId: record["senderId"],
    recipientId: record["recipientId"],
    body: record["body"],
    sentAtUtc: record["sentAtUtc"],
    read: record["read"] === true,
  };
}

/** App-scoped singleton. */
export const chatConnectionManager = new ChatConnectionManager();
