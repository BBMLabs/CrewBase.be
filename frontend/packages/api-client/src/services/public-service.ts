/** Public site service — anonymous /api/v1/public/{subdomain} surface. */
import type {
  BookAppointmentResponse,
  ConsentStateDto,
  PublicBookingRequest,
  PublicCompanyInfo,
  PublicOptionsDto,
  SlotDto,
} from "@crewbase/api-types";
import type { ApiClient } from "../http/api-client";

export interface PublicService {
  clubInfo(subdomain: string): Promise<PublicCompanyInfo>;
  options(subdomain: string): Promise<PublicOptionsDto>;
  consents(subdomain: string): Promise<readonly ConsentStateDto[]>;
  availability(
    subdomain: string,
    params: { readonly date?: string; readonly boatClass?: string; readonly phone?: string },
  ): Promise<readonly SlotDto[]>;
  bookAppointment(
    subdomain: string,
    request: PublicBookingRequest,
  ): Promise<BookAppointmentResponse>;
}

export function createPublicService(client: ApiClient): PublicService {
  const base = (subdomain: string) => `/api/v1/public/${encodeURIComponent(subdomain)}`;

  return {
    clubInfo(subdomain) {
      return client.get(`${base(subdomain)}/info`, { anonymous: true });
    },
    options(subdomain) {
      return client.get(`${base(subdomain)}/options`, { anonymous: true });
    },
    consents(subdomain) {
      return client.get(`${base(subdomain)}/consents`, { anonymous: true });
    },
    availability(subdomain, params) {
      return client.get(`${base(subdomain)}/availability`, {
        anonymous: true,
        query: { date: params.date, boatClass: params.boatClass, phone: params.phone },
      });
    },
    bookAppointment(subdomain, request) {
      return client.post(`${base(subdomain)}/appointments`, request, {
        anonymous: true,
      }) as Promise<BookAppointmentResponse>;
    },
  };
}
