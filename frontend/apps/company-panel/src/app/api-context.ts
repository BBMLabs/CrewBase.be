import { createContext, useContext } from "react";
import { createCompanyPanelApi, type CompanyPanelApi } from "@crewbase/api-client";
import { readEnv } from "./env";

let instance: CompanyPanelApi | null = null;

export function getInstance(): CompanyPanelApi {
  if (instance === null) {
    instance = createCompanyPanelApi(readEnv().apiBaseUrl);
  }
  return instance;
}

const ApiContext = createContext<CompanyPanelApi | null>(null);

export const ApiProvider = ApiContext.Provider;

export function useApi(): CompanyPanelApi {
  const value = useContext(ApiContext) ?? getInstance();
  if (value === undefined || value === null) {
    throw new Error("ApiClient kullanılabilir değil");
  }
  return value;
}
