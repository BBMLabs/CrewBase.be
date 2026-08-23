/**
 * Wire-format primitives shared by every endpoint.
 * Source: src/RowingClub.Api/ApiResponse.cs + GlobalExceptionHandler.cs
 */

/** Success envelope: { success, data, message, code }. */
export interface ApiEnvelope<T = unknown> {
  readonly success: boolean;
  readonly data: T | null;
  readonly message: string | null;
  readonly code: string | null;
}

/** Message-only envelope (data === null). */
export type ApiMessageEnvelope = ApiEnvelope<never>;

/** RFC7807 problem emitted by GlobalExceptionHandler for domain/validation failures. */
export interface ProblemDetails {
  readonly type?: string;
  readonly title?: string;
  readonly status?: number;
  readonly detail?: string;
  readonly instance?: string;
  /** Correlation id (ICorrelationIdAccessor), also sent as `traceId` extension. */
  readonly traceId?: string;
  /** Stable machine code, e.g. `slot_full`, `validation_error`. */
  readonly code?: string;
  /** FluentValidation field map — only on `code === "validation_error"`. */
  readonly errors?: Readonly<Record<string, readonly string[]>>;
  [extension: string]: unknown;
}

/** `yyyy-MM-dd` (DateOnly serialized by System.Text.Json). */
export type DateStr = string;

/** `HH:mm` (backend formats TimeOnly with ToString("HH:mm")). */
export type TimeStr = string;

/** ISO-8601 UTC instant (DateTimeOffset). */
export type TimestampIso = string;
