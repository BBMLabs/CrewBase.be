import { NavLink, Outlet, useLocation } from "react-router";
import { useQuery } from "@tanstack/react-query";
import { Button, ThemeToggle } from "@crewbase/design-system";
import { useApi } from "../../app/api-context";
import { resolveClubSubdomain } from "../../app/club";
import { useSessionStore } from "../auth/session-store";
import { useSessionSync } from "../auth/use-session-sync";

const NAV = [
  { to: "/", label: "Rezervasyon", icon: "calendar" },
  { to: "/randevularim", label: "Randevular", icon: "list" },
  { to: "/akis", label: "Akış", icon: "waves" },
  { to: "/arkadaslar", label: "Arkadaş", icon: "chat" },
  { to: "/profil", label: "Profil", icon: "user" },
] as const;

type NavIconName = (typeof NAV)[number]["icon"];

function NavIcon({ name }: { readonly name: NavIconName }) {
  const common = {
    width: 19,
    height: 19,
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: 1.8,
    strokeLinecap: "round",
    strokeLinejoin: "round",
    "aria-hidden": true,
  } as const;
  switch (name) {
    case "calendar":
      return (
        <svg {...common}>
          <rect x="3" y="4.5" width="18" height="17" rx="2.5" />
          <path d="M8 2.5v4M16 2.5v4M3 10h18M12 14v4M10 16h4" />
        </svg>
      );
    case "list":
      return (
        <svg {...common}>
          <path d="M8 6h13M8 12h13M8 18h13M3.5 6h.01M3.5 12h.01M3.5 18h.01" />
        </svg>
      );
    case "waves":
      return (
        <svg {...common}>
          <path d="M2 12c2.5 0 2.5-2 5-2s2.5 2 5 2 2.5-2 5-2 2.5 2 5 2M2 17c2.5 0 2.5-2 5-2s2.5 2 5 2 2.5-2 5-2 2.5 2 5 2" />
        </svg>
      );
    case "chat":
      return (
        <svg {...common}>
          <path d="M21 11.5a8.38 8.38 0 0 1-.9 3.8 8.5 8.5 0 0 1-7.6 4.7 8.38 8.38 0 0 1-3.8-.9L3 21l1.9-5.7a8.38 8.38 0 0 1-.9-3.8 8.5 8.5 0 0 1 4.7-7.6 8.38 8.38 0 0 1 3.8-.9h.5a8.48 8.48 0 0 1 8 8z" />
        </svg>
      );
    case "user":
      return (
        <svg {...common}>
          <circle cx="12" cy="8" r="4" />
          <path d="M4 21c.9-3.6 4-6 8-6s7.1 2.4 8 6" />
        </svg>
      );
  }
}

function initialsOf(name: string | null): string {
  if (name === null || name === "") return "··";
  const parts = name.trim().split(/\s+/);
  return ((parts[0]?.[0] ?? "") + (parts.length > 1 ? (parts[parts.length - 1]?.[0] ?? "") : "")).toUpperCase();
}

/** Subtle route transition: fade + 8px rise. Reduced-motion disables it via theme.css. */
function AnimatedOutlet() {
  const location = useLocation();
  return (
    <div key={location.pathname} className="cb-page-enter">
      <Outlet />
    </div>
  );
}

export function MemberShell() {
  useSessionSync();
  const api = useApi();
  const subdomain = resolveClubSubdomain();
  const clubName = useSessionStore((s) => s.clubName);
  const member = useSessionStore((s) => s.member);

  const clubInfo = useQuery({
    queryKey: ["public", subdomain ?? "", "info"],
    queryFn: () => api.public.clubInfo(subdomain ?? ""),
    enabled: subdomain !== null && clubName === "",
    staleTime: 10 * 60_000,
  });

  return (
    <div className="flex min-h-dvh flex-col bg-canvas">
      <header className="sticky top-0 z-[900] border-b border-line/70 bg-surface/90 backdrop-blur-md">
        <div className="mx-auto flex max-w-3xl items-center justify-between gap-3 px-4 py-3">
          <div className="flex min-w-0 items-center gap-2.5">
            <span
              aria-hidden="true"
              className="flex size-8 shrink-0 items-center justify-center rounded-lg bg-linear-to-br from-brand-400 to-brand-600 shadow-xs"
            >
              <svg width="17" height="17" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                <path
                  d="M3 16c2.2 0 2.2-1.8 4.5-1.8S9.7 16 12 16s2.3-1.8 4.5-1.8S19.8 16 21 16M6 11l6-7 6 7"
                  stroke="#fff"
                  strokeWidth="1.9"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </svg>
            </span>
            <div className="min-w-0 leading-tight">
              <p className="truncate font-display text-[15px] font-extrabold tracking-[-0.01em] text-ink">
                {clubName !== "" ? clubName : (clubInfo.data?.name ?? "Üye Portalı")}
              </p>
              <p className="text-[10.5px] font-medium uppercase tracking-[0.08em] text-ink-3">CrewBase üye</p>
            </div>
          </div>
          <div className="flex shrink-0 items-center gap-2.5">
            <span
              aria-hidden="true"
              className="hidden size-8 items-center justify-center rounded-full bg-brand-50 text-[11px] font-bold text-brand-700 sm:flex"
            >
              {initialsOf(member?.fullName ?? null)}
            </span>
            <ThemeToggle />
            <Button
              variant="ghost"
              size="sm"
              onClick={() => {
                api.session.clear();
                window.location.assign("/giris");
              }}
            >
              Çıkış
            </Button>
          </div>
        </div>
      </header>

      <main className="mx-auto w-full max-w-3xl flex-1 px-4 pb-28 pt-6 md:pb-10">
        <AnimatedOutlet />
      </main>

      {/* Mobile bottom nav */}
      <nav
        aria-label="Ana menü"
        className="fixed inset-x-0 bottom-0 z-[900] border-t border-line/70 bg-surface/95 backdrop-blur-md md:hidden"
      >
        <ul className="mx-auto flex max-w-3xl">
          {NAV.map((item) => (
            <li key={item.to} className="flex-1">
              <NavLink
                to={item.to}
                end={item.to === "/"}
                className={({ isActive }) =>
                  [
                    "flex flex-col items-center gap-1 py-2.5 text-[10.5px] font-semibold transition-colors",
                    isActive ? "text-brand-600" : "text-ink-3 hover:text-ink-2",
                  ].join(" ")
                }
              >
                {({ isActive }) => (
                  <>
                    <span
                      className={[
                        "rounded-full px-3.5 py-1 transition-colors",
                        isActive ? "bg-brand-50" : "",
                      ].join(" ")}
                    >
                      <NavIcon name={item.icon} />
                    </span>
                    {item.label}
                  </>
                )}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>

      {/* Desktop inline nav */}
      <nav aria-label="Ana menü" className="sticky bottom-4 hidden justify-center md:flex">
        <ul className="flex gap-1 rounded-full border border-line/80 bg-surface/95 p-1.5 shadow-md backdrop-blur">
          {NAV.map((item) => (
            <li key={item.to}>
              <NavLink
                to={item.to}
                end={item.to === "/"}
                className={({ isActive }) =>
                  [
                    "flex items-center gap-2 rounded-full px-4 py-2 text-[13px] font-semibold transition-all",
                    isActive ? "bg-rail text-white shadow-xs" : "text-ink-2 hover:bg-surface-2",
                  ].join(" ")
                }
              >
                <NavIcon name={item.icon} />
                {item.label}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>
    </div>
  );
}
