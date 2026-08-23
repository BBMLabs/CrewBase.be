/**
 * Normalized error contract for every API failure mode.
 * Backend sources: ApiResponse.cs (envelope failures) and
 * GlobalExceptionHandler.cs (RFC7807 ProblemDetails with code/traceId/errors).
 */
import type { ProblemDetails } from "@crewbase/api-types";

export type AppErrorKind = "problem" | "envelope" | "network" | "timeout" | "cancelled";

export class AppError extends Error {
  readonly kind: AppErrorKind;
  /** Stable backend code when available (`slot_full`, `unauthorized`, `validation_error`…). */
  readonly code: string | undefined;
  readonly status: number | undefined;
  readonly traceId: string | undefined;
  /** Field→messages map from FluentValidation (`code === "validation_error"`). */
  readonly fieldErrors: Readonly<Record<string, readonly string[]>> | undefined;
  /** Server-provided human message (Turkish) — fallback UX copy. */
  readonly serverMessage: string | undefined;

  constructor(init: {
    kind: AppErrorKind;
    message: string;
    code?: string;
    status?: number;
    traceId?: string;
    fieldErrors?: Readonly<Record<string, readonly string[]>>;
    serverMessage?: string;
    cause?: unknown;
  }) {
    super(init.message, init.cause === undefined ? undefined : { cause: init.cause });
    this.name = "AppError";
    this.kind = init.kind;
    this.code = init.code;
    this.status = init.status;
    this.traceId = init.traceId;
    this.fieldErrors = init.fieldErrors;
    this.serverMessage = init.serverMessage;
  }

  get isUnauthorized(): boolean {
    return this.status === 401 || this.code === "unauthorized";
  }

  get isForbidden(): boolean {
    return this.status === 403 || (this.kind !== "network" && this.code === "forbidden");
  }
}

/** HTTP status codes that carry no parsable JSON body we care about. */
function emptyStatus(status: number): boolean {
  return status === 204 || status === 205;
}

function isProblemDetails(body: unknown): body is ProblemDetails {
  return (
    typeof body === "object" && body !== null &&
    !("success" in body) &&
    ("status" in body || "traceId" in body || "errors" in body || "title" in body)
  );
}

interface EnvelopeFailureShape {
  readonly success: false;
  readonly message: string | null;
  readonly code: string | null;
}

function isEnvelopeFailure(body: unknown): body is EnvelopeFailureShape {
  return (
    typeof body === "object" && body !== null &&
    "success" in body && (body as { success: unknown }).success === false
  );
}

/** Build an AppError from a fetch Response + parsed-or-null body. */
export function errorFromResponse(
  res: Response,
  body: unknown,
  fallbackMessage: string,
  cause?: unknown,
): AppError {
  if (isEnvelopeFailure(body)) {
    return new AppError({
      kind: "envelope",
      message: body.message ?? fallbackMessage,
      code: body.code ?? undefined,
      status: res.status,
      serverMessage: body.message ?? undefined,
      cause,
    });
  }

  if (isProblemDetails(body)) {
    return new AppError({
      kind: "problem",
      // detail is the server's Turkish business text; title is generic ("Conflict" etc).
      message: body.detail ?? body.title ?? fallbackMessage,
      code: body.code,
      status: body.status ?? res.status,
      traceId: body.traceId,
      fieldErrors: body.errors,
      serverMessage: body.detail ?? body.title,
      cause,
    });
  }

  return new AppError({
    kind: "problem",
    message: fallbackMessage,
    status: res.status,
    serverMessage: undefined,
    cause,
  });
}

export function networkError(cause: unknown): AppError {
  return new AppError({ kind: "network", message: "Sunucuya ulaşılamadı.", cause });
}

export function timeoutError(): AppError {
  return new AppError({ kind: "timeout", message: "İstek zaman aşımına uğradı." });
}

export function cancelledError(): AppError {
  return new AppError({ kind: "cancelled", message: "İstek iptal edildi." });
}

export { emptyStatus };
