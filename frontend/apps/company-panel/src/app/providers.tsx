import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useState } from "react";
import { ApiProvider } from "./api-context";
import { useSessionSync } from "../features/auth/use-session-sync";
import type { CompanyPanelApi } from "@crewbase/api-client";

/** Retry policy: none (mutations non-idempotent; auth endpoints rate-limited 10/min/IP). */
function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        staleTime: 30_000,
        refetchOnWindowFocus: true,
      },
      mutations: {
        retry: false,
      },
    },
  });
}

/**
 * Hydrates the session store from the token kernel on mount and keeps the proactive
 * refresh timer alive for the whole app lifetime. Without this the guard would sit
 * in `status === "unknown"` forever.
 */
function SessionBootstrapper() {
  useSessionSync();
  return null;
}

export function AppProviders({
  api,
  children,
}: {
  readonly api: CompanyPanelApi;
  readonly children: React.ReactNode;
}) {
  const [queryClient] = useState(createQueryClient);
  return (
    <QueryClientProvider client={queryClient}>
      <ApiProvider value={api}>
        <SessionBootstrapper />
        {children}
      </ApiProvider>
    </QueryClientProvider>
  );
}
