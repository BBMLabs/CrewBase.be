/** Auth service — anonymous account-recovery flows. Login/refresh/logout live in the session strategies. */
import type {
  RegisterCompanyRequest,
  RegisterCompanyResponse,
  ResetPasswordRequest,
  VerifyEmailRequest,
} from "@crewbase/api-types";
import type { ApiClient } from "../http/api-client";

export interface AuthService {
  registerCompany(request: RegisterCompanyRequest): Promise<RegisterCompanyResponse>;
  forgotPassword(email: string): Promise<string>;
  resetPassword(request: ResetPasswordRequest): Promise<string>;
  verifyEmail(request: VerifyEmailRequest): Promise<string>;
  sendVerificationEmail(email: string): Promise<string>;
}

export function createAuthService(client: ApiClient): AuthService {
  return {
    async registerCompany(request) {
      return client.post<RegisterCompanyRequest, RegisterCompanyResponse>(
        "/api/v1/auth/companies/register",
        request,
        { anonymous: true },
      );
    },
    async forgotPassword(email) {
      const message = await client.post<{ email: string }, { message: string | null }>(
        "/api/v1/auth/forgot-password",
        { email },
        { anonymous: true },
      );
      return message?.message ?? "";
    },
    async resetPassword(request) {
      const message = await client.post<ResetPasswordRequest, { message: string | null }>(
        "/api/v1/auth/reset-password",
        request,
        { anonymous: true },
      );
      return message?.message ?? "";
    },
    async verifyEmail(request) {
      const message = await client.post<VerifyEmailRequest, { message: string | null }>(
        "/api/v1/auth/verify-email",
        request,
        { anonymous: true },
      );
      return message?.message ?? "";
    },
    async sendVerificationEmail(email) {
      const message = await client.post<{ email: string }, { message: string | null }>(
        "/api/v1/auth/send-verification-email",
        { email },
        { anonymous: true },
      );
      return message?.message ?? "";
    },
  };
}
