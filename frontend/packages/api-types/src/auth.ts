/** Auth endpoints — source: src/RowingClub.Api/Endpoints/AuthEndpoints.cs + Identity.Application records. */
import type { TimestampIso } from "./envelope";

export interface RegisterCompanyRequest {
  readonly companyName: string;
  readonly adminEmail: string;
  readonly adminPassword: string;
  readonly phone?: string | null;
  readonly contactEmail?: string | null;
  readonly address?: string | null;
}

/** RegisterCompanyCommand.cs RegisterCompanyResponse. */
export interface RegisterCompanyResponse {
  readonly companyId: string;
  readonly adminUserId: string;
  readonly companyName: string;
  readonly adminEmail: string;
  readonly subdomain: string;
  readonly siteUrl: string;
  readonly createdAtUtc: TimestampIso;
}

export interface LoginRequest {
  readonly email: string;
  readonly password: string;
}

/** LoginCommand.cs LoginResult — 2FA fields present but always false/null (routes disabled). */
export interface LoginResult {
  readonly requiresTwoFactor: boolean;
  readonly pendingTwoFactorToken: string | null;
  readonly accessToken: string | null;
  readonly accessTokenExpiresAtUtc: TimestampIso | null;
  readonly refreshToken: string | null;
}

/** RefreshTokenCommand.cs RefreshTokenResponse. */
export interface RefreshTokenResponse {
  readonly accessToken: string;
  readonly accessTokenExpiresAtUtc: TimestampIso;
  readonly refreshToken: string;
}

export interface RefreshTokenRequest {
  readonly refreshToken: string;
}

export interface ForgotPasswordRequest {
  readonly email: string;
}

export interface ResetPasswordRequest {
  readonly email: string;
  readonly token: string;
  readonly newPassword: string;
}

/** Token = 6-digit OTP code sent by e-mail. */
export interface VerifyEmailRequest {
  readonly email: string;
  readonly token: string;
}
