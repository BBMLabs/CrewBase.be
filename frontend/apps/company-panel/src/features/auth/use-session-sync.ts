import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useApi } from "../../app/api-context";
import { useSessionStore } from "./session-store";

const REFRESH_CHECK_INTERVAL_MS = 60_000;

/**
 * Hydrates the zustand session store from the token kernel on mount and keeps it in
 * sync afterwards: proactive single-flight refresh shortly before access-token expiry
 * (kernel: expiry − 60s), periodic safety check, and hard-logout when refresh fails.
 */
export function useSessionSync(): void {
  const api = useApi();
  const queryClient = useQueryClient();
  const applySnapshot = useSessionStore((s) => s.applySnapshot);
  const markGuest = useSessionStore((s) => s.markGuest);

  useEffect(() => {
    applySnapshot(api.session.snapshot());
  }, [api, applySnapshot]);

  useEffect(() => {
    let disposed = false;
    let timer: ReturnType<typeof setTimeout> | undefined;

    const attemptRefresh = () => {
      if (disposed) return;
      if (!api.session.hasRefreshToken()) {
        scheduleNext();
        return;
      }
      void api.session.refreshNow().then((ok) => {
        if (disposed) return;
        if (!ok) {
          queryClient.clear();
          markGuest();
        } else {
          applySnapshot(api.session.snapshot());
        }
        scheduleNext();
      });
    };

    const scheduleNext = () => {
      if (disposed) return;
      const due = Math.max(5_000, api.session.msUntilRefreshDue() || REFRESH_CHECK_INTERVAL_MS);
      timer = setTimeout(attemptRefresh, Math.min(due, REFRESH_CHECK_INTERVAL_MS));
    };

    scheduleNext();
    return () => {
      disposed = true;
      if (timer !== undefined) clearTimeout(timer);
    };
  }, [api, applySnapshot, markGuest, queryClient]);
}
