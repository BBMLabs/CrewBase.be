import { createContext, useContext } from "react";
import { createMemberPortalApi, type MemberPortalApi } from "@crewbase/api-client";
import { API_BASE_URL } from "./env";

let instance: MemberPortalApi | null = null;

export function getInstance(): MemberPortalApi {
  if (instance === null) instance = createMemberPortalApi(API_BASE_URL);
  return instance;
}

const ApiContext = createContext<MemberPortalApi | null>(null);

export const ApiProvider = ApiContext.Provider;

export function useApi(): MemberPortalApi {
  const value = useContext(ApiContext) ?? getInstance();
  if (value === null) throw new Error("ApiClient kullanılabilir değil");
  return value;
}
