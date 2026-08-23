import { useState, type ReactNode } from "react";
import { NavLink, Outlet, useLocation } from "react-router";
import { useMutation, useQuery } from "@tanstack/react-query";
import { Button, ConfirmDialog, ThemeToggle } from "@crewbase/design-system";
import { useApi } from "../../app/api-context";
import { panelKeys } from "../../shared/query-keys";
import { useSessionStore } from "../auth/session-store";

type IconName =
  | "overview"
  | "calendar"
  | "members"
  | "package"
  | "boat"
  | "feed"
  | "users"
  | "settings";

function NavIcon({ name }: { readonly name: IconName }): ReactNode {
  const common = {
    width: 17,
    height: 17,
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: 1.8,
    strokeLinecap: "round",
    strokeLinejoin: "round",
    "aria-hidden": true,
  } as const;
  switch (name) {
    case "overview":
      return (
        <svg {...common}>
          <rect x="3" y="3" width="7" height="9" rx="1.5" />
          <rect x="14" y="3" width="7" height="5" rx="1.5" />
          <rect x="14" y="12" width="7" height="9" rx="1.5" />
          <rect x="3" y="16" width="7" height="5" rx="1.5" />
        </svg>
      );
    case "calendar":
      return (
        <svg {...common}>
          <rect x="3" y="4.5" width="18" height="17" rx="2.5" />
          <path d="M8 2.5v4M16 2.5v4M3 10h18" />
        </svg>
      );
    case "members":
      return (
        <svg {...common}>
          <circle cx="9" cy="8" r="3.5" />
          <path d="M2.5 20c.8-3.2 3.4-5 6.5-5s5.7 1.8 6.5 5" />
          <path d="M16.5 4.6a3.5 3.5 0 0 1 0 6.8M18 15.2c2 .8 3.2 2.4 3.6 4.8" />
        </svg>
      );
    case "package":
      return (
        <svg {...common}>
          <path d="M21 8.5v9L12 22 3 17.5v-9L12 4z" />
          <path d="M3 8.5 12 13l9-4.5M12 13v9M7.5 6.25 16.5 11" />
        </svg>
      );
    case "boat":
      return (
        <svg {...common}>
          <path d="M4 18h16l-2.5 3h-11z" />
          <path d="M12 3v15M12 6c3 0 5.5-1.2 6-4-3.5-.5-5.5.8-6 4zM12 6C9 6 6.5 4.8 6 2c3.5-.5 5.5.8 6 4z" />
        </svg>
      );
    case "feed":
      return (
        <svg {...common}>
          <path d="M2 12c2.5 0 2.5-2 5-2s2.5 2 5 2 2.5-2 5-2 2.5 2 5 2" />
          <path d="M2 17c2.5 0 2.5-2 5-2s2.5 2 5 2 2.5-2 5-2 2.5 2 5 2" />
          <path d="M2 7c2.5 0 2.5-2 5-2s2.5 2 5 2 2.5-2 5-2 2.5 2 5 2" />
        </svg>
      );
    case "users":
      return (
        <svg {...common}>
          <circle cx="12" cy="12" r="3" />
          <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 1 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 1 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 1 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 1 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z" />
        </svg>
      );
    case "settings":
      return (
        <svg {...common}>
          <path d="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z" />
          <circle cx="12" cy="12" r="3" />
        </svg>
      );
  }
}

interface NavItem {
  readonly to: string;
  readonly label: string;
  readonly icon: IconName;
  readonly end?: boolean;
}

const NAV_GROUPS: ReadonlyArray<{ title: string; items: readonly NavItem[] }> = [
  {
    title: "Operasyon",
    items: [
      { to: "/", label: "Genel Bakış", icon: "overview", end: true },
      { to: "/program", label: "Gün Programı", icon: "calendar" },
      { to: "/uyeler", label: "Üyeler", icon: "members" },
    ],
  },
  {
    title: "Kulüp",
    items: [
      { to: "/paketler", label: "Paketler", icon: "package" },
      { to: "/kaynaklar", label: "Kaynaklar", icon: "boat" },
      { to: "/akis", label: "Kulüp Akışı", icon: "feed" },
    ],
  },
  {
    title: "Yönetim",
    items: [
      { to: "/kullanicilar", label: "Kullanıcılar", icon: "users" },
      { to: "/ayarlar", label: "Ayarlar", icon: "settings" },
    ],
  },
];

function initialsOf(value: string | null): string {
  if (value === null || value === "") return "··";
  const parts = value.trim().split(/\s+/);
  return ((parts[0]?.[0] ?? "") + (parts.length > 1 ? (parts[parts.length - 1]?.[0] ?? "") : "")).toUpperCase();
}

/** Subtle route transition: fade + 8px rise, keyed by pathname. Reduced-motion disables it. */
function AnimatedOutlet(): ReactNode {
  const location = useLocation();
  return (
    <div key={location.pathname} className="cb-page-enter">
      <Outlet />
    </div>
  );
}

export function AppShell() {
  const api = useApi();
  const email = useSessionStore((s) => s.email);
  const role = useSessionStore((s) => s.role);
  const companyId = useSessionStore((s) => s.companyId);
  const [confirmLogout, setConfirmLogout] = useState<"single" | "all" | null>(null);

  const siteQuery = useQuery({
    queryKey: panelKeys.site(companyId),
    queryFn: () => api.company.site(),
    retry: false,
    staleTime: 5 * 60_000,
  });

  const logout = useMutation({
    mutationFn: async (mode: "single" | "all") => {
      if (mode === "all") await api.session.logoutAll();
      else await api.session.logout();
    },
    onSettled: () => {
      api.session.clearLocal();
      window.location.assign("/giris");
    },
  });

  const roleLabel =
    role === "CompanyAdmin" ? "Firma Yöneticisi" : role === "Employee" ? "Çalışan" : (role ?? "");

  return (
    <div className="flex min-h-dvh bg-[radial-gradient(circle_at_top,_rgba(29,116,201,0.16),transparent_28%),radial-gradient(circle_at_bottom_right,_rgba(239,141,74,0.12),transparent_20%),linear-gradient(180deg,_rgba(247,250,252,0.92),_var(--color-canvas)_18%,_var(--color-canvas)_100%)]">
      <a
        href="#main-content"
        className="sr-only focus:not-sr-only focus:absolute focus:left-2 focus:top-2 focus:z-[1400] focus:rounded-md focus:bg-surface focus:px-3 focus:py-2 focus:text-sm"
      >
        İçeriğe geç
      </a>

      {/* ── Rail ─────────────────────────────────────────── */}
      <aside className="sticky top-0 hidden h-dvh w-[236px] shrink-0 flex-col border-r border-white/10 bg-[linear-gradient(180deg,_rgba(9,24,36,0.98),_rgba(8,29,45,0.97))] shadow-[16px_0_54px_-30px_rgba(4,13,20,0.96)] lg:flex">
        {/* Brand */}
        <div className="flex items-center gap-2.5 px-4 pb-5 pt-5">
          <span
            aria-hidden="true"
            className="flex size-8 shrink-0 items-center justify-center rounded-md bg-linear-to-br from-brand-400 to-brand-600 shadow-sm"
          >
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <path
                d="M3 16c2.2 0 2.2-1.8 4.5-1.8S9.7 16 12 16s2.3-1.8 4.5-1.8S19.8 16 21 16M6 11l6-7 6 7"
                stroke="#fff"
                strokeWidth="1.9"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </svg>
          </span>
          <div className="min-w-0 flex-1 leading-tight">
            <p className="font-display text-[15px] font-extrabold tracking-[-0.01em] text-white">CrewBase</p>
            <p className="truncate text-[11px] font-medium text-rail-text">
              {siteQuery.data?.name ?? "Kulüp paneli"}
            </p>
          </div>
          <ThemeToggle tone="onDark" />
        </div>

        {/* Nav groups */}
        <nav aria-label="Ana menü" className="flex-1 overflow-y-auto px-2.5 pb-4">
          {NAV_GROUPS.map((group) => (
            <div key={group.title} className="mb-5">
              <p className="px-2.5 pb-1.5 text-[10.5px] font-bold uppercase tracking-[0.09em] text-rail-text/70">
                {group.title}
              </p>
              <ul className="flex flex-col gap-0.5">
                {group.items.map((item) => (
                  <li key={item.to}>
                    <NavLink
                      to={item.to}
                      end={item.end ?? false}
                      className={({ isActive }) =>
                        [
                          "group relative flex items-center gap-2.5 rounded-md px-2.5 py-2 text-[13.5px] font-medium transition-colors duration-150",
                          isActive
                            ? "bg-rail-active text-white"
                            : "text-rail-text hover:bg-rail-hover hover:text-white/90",
                        ].join(" ")
                      }
                    >
                      {({ isActive }) => (
                        <>
                          <span
                            aria-hidden="true"
                            className={[
                              "absolute left-0 top-1/2 h-4 w-[3px] -translate-y-1/2 rounded-r-full",
                              isActive ? "bg-oar-500" : "bg-transparent",
                            ].join(" ")}
                          />
                          <span className={isActive ? "text-oar-300" : "text-rail-text group-hover:text-white"}>
                            <NavIcon name={item.icon} />
                          </span>
                          {item.label}
                        </>
                      )}
                    </NavLink>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </nav>

        {/* User card */}
        <div className="border-t border-white/8 p-3">
          <div className="flex items-center gap-2.5 rounded-md bg-rail-hover/70 px-2.5 py-2.5">
            <span className="flex size-8 shrink-0 items-center justify-center rounded-full bg-brand-500/90 text-[12px] font-bold text-white">
              {initialsOf(email)}
            </span>
            <div className="min-w-0 flex-1 leading-tight">
              <p className="truncate text-[12.5px] font-semibold text-white">{email ?? ""}</p>
              <p className="truncate text-[11px] text-rail-text">{roleLabel}</p>
            </div>
            <button
              type="button"
              onClick={() => setConfirmLogout("single")}
              aria-label="Çıkış yap"
              title="Çıkış yap"
              className="rounded-md p-1.5 text-rail-text transition-colors hover:bg-white/10 hover:text-white"
            >
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4M16 17l5-5-5-5M21 12H9" />
              </svg>
            </button>
          </div>
        </div>
      </aside>

      {/* ── Content ──────────────────────────────────────── */}
      <div className="flex min-w-0 flex-1 flex-col">
        {/* Mobile topbar */}
        <header className="sticky top-0 z-[900] border-b border-line/80 bg-surface/85 backdrop-blur-xl lg:hidden">
          <div className="flex items-center justify-between px-4 py-3">
            <div className="min-w-0">
              <p className="font-display text-sm font-extrabold tracking-[-0.01em] text-ink">CrewBase</p>
              <p className="truncate text-[11px] text-ink-3">{siteQuery.data?.name ?? email ?? ""}</p>
            </div>
            <div className="flex shrink-0 items-center gap-1.5">
              <ThemeToggle />
              <Button variant="secondary" size="sm" onClick={() => setConfirmLogout("single")}>
                Çıkış
              </Button>
            </div>
          </div>
          <nav aria-label="Ana menü" className="overflow-x-auto px-2 pb-2">
            <ul className="flex gap-1">
              {NAV_GROUPS.flatMap((g) => g.items).map((item) => (
                <li key={item.to} className="shrink-0">
                  <NavLink
                    to={item.to}
                    end={item.end ?? false}
                    className={({ isActive }) =>
                      [
                        "block rounded-md px-3 py-1.5 text-[12.5px] font-medium",
                        isActive ? "bg-brand-50 text-brand-700" : "text-ink-2 hover:bg-surface-2",
                      ].join(" ")
                    }
                  >
                    {item.label}
                  </NavLink>
                </li>
              ))}
            </ul>
          </nav>
        </header>

        <main id="main-content" className="mx-auto w-full max-w-[1320px] flex-1 px-4 py-6 md:px-8 md:py-8">
          <div className="app-shell-surface rounded-[30px] p-3 shadow-[0_24px_60px_-32px_rgba(15,29,43,0.55)] md:p-4">
            <div className="glass-panel rounded-[24px] p-2 md:p-3">
              <AnimatedOutlet />
            </div>
          </div>
        </main>
      </div>

      <ConfirmDialog
        open={confirmLogout !== null}
        onClose={() => setConfirmLogout(null)}
        title="Çıkış yapılsın mı?"
        description={
          confirmLogout === "all"
            ? "Bu hesabın tüm cihazlardaki oturumları kapatılacak."
            : "Bu cihazdaki oturumunuz kapatılacak."
        }
        confirmLabel={confirmLogout === "all" ? "Tümünden çık" : "Çıkış yap"}
        danger
        pending={logout.isPending}
        onConfirm={() => {
          if (confirmLogout !== null) logout.mutate(confirmLogout);
        }}
      />
    </div>
  );
}
