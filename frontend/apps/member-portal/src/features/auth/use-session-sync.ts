import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useApi } from "../../app/api-context";
import { useSessionStore } from "./session-store";

/**
 * Hydrates session from sessionStorage, registers the expiry listener (members have NO
 * refresh endpoint — kernel clears storage and emits), and schedules an expiry check.
 */
export function useSessionSync(): void {
  const api = useApi();
  const queryClient = useQueryClient();
  const applySession = useSessionStore((s) => s.applySession);
  const markExpired = useSessionStore((s) => s.markExpired);
  const markGuest = useSessionStore((s) => s.markGuest);

  useEffect(() => {
    // Boot-time state machine: every branch must exit "unknown".
    // absent → guest | expired → expired | valid → authenticated
    const current = api.session.current();
    if (current === null) {
      markGuest();
      return;
    }
    if (api.session.isExpired(current)) {
      api.session.clear();
      markExpired();
      return;
    }
    applySession(current);
  }, [api, applySession, markExpired, markGuest]);

  useEffect(() => {
    const offExpired = api.session.onExpired(() => {
      queryClient.clear();
      markExpired();
    });

    const interval = setInterval(() => {
      const current = api.session.current();
      if (current !== null && api.session.isExpired(current)) {
        api.session.getAccessToken(); // triggers clear + emit
      }
    }, 30_000);

    return () => {
      offExpired();
      clearInterval(interval);
    };
  }, [api, markExpired, queryClient]);
}
