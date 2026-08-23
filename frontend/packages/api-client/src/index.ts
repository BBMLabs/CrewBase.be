export { ApiClient, createCorrelationId } from "./http/api-client";
export type { ApiClientConfig, AuthHooks, RequestOptions } from "./http/api-client";
export { AppError, errorFromResponse } from "./http/app-error";
export type { AppErrorKind } from "./http/app-error";
export { catalogLookup } from "./http/error-catalog";
export type { ErrorCopy } from "./http/error-catalog";

export { CompanyUserSession } from "./auth/company-user-session";
export type { CompanySessionSnapshot } from "./auth/company-user-session";
export { MemberSession } from "./auth/member-session";
export type { MemberStoredSession } from "./auth/member-session";
export { companyAccessStorage, companyExpiryStorage, companyRefreshStorage, memberSessionStorage } from "./auth/storage";
export type { TokenStorage } from "./auth/storage";
export { decodeJwtPayload } from "./auth/jwt";
export type { JwtPayload } from "./auth/jwt";

export { BASE_DOMAIN, currentSubdomain, subdomainFromHost } from "./tenant/subdomain";

export { createAuthService } from "./services/auth-service";
export type { AuthService } from "./services/auth-service";
export { createCompanyService } from "./services/company-service";
export type { CompanyService } from "./services/company-service";
export { createMemberService } from "./services/member-service";
export type { MemberService } from "./services/member-service";
export { createPublicService } from "./services/public-service";
export type { PublicService } from "./services/public-service";

import { ApiClient, createCorrelationId } from "./http/api-client";
import { CompanyUserSession } from "./auth/company-user-session";
import { MemberSession } from "./auth/member-session";
import { createAuthService, type AuthService } from "./services/auth-service";
import { createCompanyService, type CompanyService } from "./services/company-service";
import { createMemberService, type MemberService } from "./services/member-service";
import { createPublicService, type PublicService } from "./services/public-service";

export function createCompanyPanelApi(baseUrl: string): CompanyPanelApi {
  const holder: { session?: CompanyUserSession } = {};
  const client = new ApiClient({
    baseUrl,
    correlationId: createCorrelationId(),
    authHooks: {
      getAccessToken: () => holder.session?.getAccessToken() ?? null,
      onUnauthorized: (error) =>
        holder.session ? holder.session.onUnauthorized(error) : Promise.resolve(false),
    },
  });
  const session = new CompanyUserSession(client);
  holder.session = session;
  return {
    client,
    session,
    auth: createAuthService(client),
    company: createCompanyService(client),
  };
}

export interface CompanyPanelApi {
  readonly client: ApiClient;
  readonly session: CompanyUserSession;
  readonly auth: AuthService;
  readonly company: CompanyService;
}

export interface MemberPortalApi {
  readonly client: ApiClient;
  readonly session: MemberSession;
  readonly member: MemberService;
  readonly public: PublicService;
}

export function createMemberPortalApi(baseUrl: string): MemberPortalApi {
  const holder: { session?: MemberSession } = {};
  const client = new ApiClient({
    baseUrl,
    correlationId: createCorrelationId(),
    authHooks: {
      getAccessToken: () => holder.session?.getAccessToken() ?? null,
      onUnauthorized: () => (holder.session ? holder.session.onUnauthorized() : Promise.resolve(false)),
    },
  });
  const session = new MemberSession(client);
  holder.session = session;
  return { client, session, member: createMemberService(client), public: createPublicService(client) };
}
