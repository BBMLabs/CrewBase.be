import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useState } from "react";
import { useSessionSync } from "../features/auth/use-session-sync";

/** Retry policy: none (member auth is rate-limited; no idempotency keys exist). */
function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false, staleTime: 30_000, refetchOnWindowFocus: true },
      mutations: { retry: false },
    },
  });
}

/**
 * Hydrates the session store from sessionStorage before any route guard runs.
 * Without this the guard would sit in `status === "unknown"` forever (chicken-and-egg:
 * the previous mount point lived behind the guard itself).
 */
function SessionBootstrapper() {
  useSessionSync();
  return null;
}

export function AppProviders({ children }: { readonly children: React.ReactNode }) {
  const [queryClient] = useState(createQueryClient);
  return (
    <QueryClientProvider client={queryClient}>
      <SessionBootstrapper />
      {children}
    </QueryClientProvider>
  );
}
