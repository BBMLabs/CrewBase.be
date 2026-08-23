/** E2E environment + backend readiness helpers. Real backend only — no mocks. */

export const API_URL = process.env["E2E_API_URL"] ?? "http://localhost:5283";
export const BASE_URL = process.env["E2E_BASE_URL"] ?? "http://localhost:5173";

const MEMBER_SUBDOMAIN =
  process.env["E2E_MEMBER_SUBDOMAIN"] ?? process.env["VITE_CLUB_SUBDOMAIN"] ?? "";

export interface CompanyCredentials {
  readonly email: string;
  readonly password: string;
}

export function companyCredentials(): CompanyCredentials {
  const email = process.env["E2E_COMPANY_EMAIL"];
  const password = process.env["E2E_COMPANY_PASSWORD"];
  if (email === undefined || password === undefined) {
    throw new Error(
      "E2E_COMPANY_EMAIL / E2E_COMPANY_PASSWORD tanımlı değil. Gerçek backend'e karşı çalışmak için bir CompanyAdmin test hesabı gerekir.",
    );
  }
  return { email, password };
}

export function memberSubdomain(): string {
  if (MEMBER_SUBDOMAIN === "") {
    throw new Error("E2E_MEMBER_SUBDOMAIN tanımlı değil — üye portalı akışları için kulüp alt alan adı gerekir.");
  }
  return MEMBER_SUBDOMAIN;
}

interface HealthResponse {
  readonly status?: string;
}

/** Probes GET /health; throws with setup instructions when unreachable. */
export async function requireBackend(apiUrl: string): Promise<void> {
  try {
    const response = await fetch(`${apiUrl}/health`, { signal: AbortSignal.timeout(5_000) });
    if (!response.ok) {
      throw new Error(`health ${response.status}`);
    }
    const body = (await response.json()) as HealthResponse;
    if (body.status !== undefined && body.status !== "Healthy") {
      throw new Error(`backend sağlıksız: ${body.status}`);
    }
  } catch (error) {
    throw new Error(
      `Backend erişilemiyor (${apiUrl}/health): ${error instanceof Error ? error.message : String(error)}\n` +
        "Gerçek-backend modunda test koşmak için:\n" +
        "  1) .env.developer doldurun\n" +
        "  2) dotnet run --project src/RowingClub.Api\n" +
        "  3) npm run dev -w @crewbase/company-panel (ve gerekiyorsa diğer uygulamalar)\n" +
        "Mock fallback YOKTUR — bilinçli tasarım.",
    );
  }
}

/** Authenticates through the REAL /auth/login and returns the bearer token pair. */
export async function loginCompany(credentials: CompanyCredentials): Promise<{
  accessToken: string;
  refreshToken: string;
}> {
  const response = await fetch(`${API_URL}/api/v1/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(credentials),
    signal: AbortSignal.timeout(10_000),
  });
  const body = (await response.json()) as {
    success: boolean;
    data?: { accessToken?: string | null; refreshToken?: string | null };
  };
  if (!response.ok || body.success !== true || body.data?.accessToken == null || body.data.refreshToken == null) {
    throw new Error("CompanyAdmin girişi başarısız — E2E kimlik bilgilerini kontrol edin.");
  }
  return { accessToken: body.data.accessToken, refreshToken: body.data.refreshToken };
}
