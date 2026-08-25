/**
 * Minimal JWT payload reader for UX decisions only (role/company/expiry display).
 * NOT a security boundary — the backend validates every request.
 */

export interface JwtPayload {
  readonly sub: string | null;
  readonly email: string | null;
  /** `role` claim (ClaimTypes.Role) — PlatformAdmin|CompanyAdmin|Employee|Member. */
  readonly role: string | null;
  readonly companyId: string | null;
  /** Unix seconds. */
  readonly exp: number | null;

  [claim: string]: unknown;
}

export function decodeJwtPayload(token: string): JwtPayload | null {
  const parts = token.split(".");
  if (parts.length !== 3) return null;
  try {
    const json = atob(parts[1]!.replace(/-/g, "+").replace(/_/g, "/"));
    const parsed: unknown = JSON.parse(
      decodeURIComponent(
        json
          .split("")
          .map((c) => `%${`00${c.charCodeAt(0).toString(16)}`.slice(-2)}`)
          .join(""),
      ),
    );
    if (typeof parsed !== "object" || parsed === null) return null;
    const record = parsed as Record<string, unknown>;
    return {
      sub: asString(record["sub"]),
      email: asString(record["email"]),
      role: asString(record["role"]),
      companyId: asString(record["company_id"]),
      exp: typeof record["exp"] === "number" ? record["exp"] : null,
      ...record,
    };
  } catch {
    return null;
  }
}

function asString(value: unknown): string | null {
  return typeof value === "string" ? value : null;
}
