/** Platform admin DTOs — source: Identity.Application/Platform/PlatformQueries.cs.
 *  Not consumed by any app this pass (reserved for future apps/platform-admin). */
import type { CompanyStatus } from "./common";
import type { TimestampIso } from "./envelope";

export interface PlatformCompanyDto {
  readonly id: string;
  readonly name: string;
  readonly subdomain: string;
  readonly status: CompanyStatus;
  readonly phone: string | null;
  readonly contactEmail: string | null;
  readonly createdAtUtc: TimestampIso;
}

export interface PlatformStatsDto {
  readonly totalCompanies: number;
  readonly activeCompanies: number;
  readonly suspendedCompanies: number;
  readonly registeredThisMonth: number;
}

export interface PendingCompanyDto {
  readonly companyId: string;
  readonly name: string;
  readonly contactEmail: string | null;
  readonly phone: string | null;
  readonly address: string | null;
  readonly createdAtUtc: TimestampIso;
}
