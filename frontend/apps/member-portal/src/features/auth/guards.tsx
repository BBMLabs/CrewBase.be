import { Navigate, Outlet, useLocation } from "react-router";
import { useSessionStore } from "./session-store";

export function RequireMember(): React.JSX.Element {
  const status = useSessionStore((s) => s.status);
  const location = useLocation();

  if (status === "unknown") {
    return <div className="flex min-h-dvh items-center justify-center text-sm text-ink-3">Yükleniyor…</div>;
  }
  if (status === "expired") {
    return <Navigate to="/giris?oturum=sona-erdi" replace state={{ from: location.pathname }} />;
  }
  if (status !== "authenticated") {
    return <Navigate to="/giris" replace state={{ from: location.pathname }} />;
  }
  return <Outlet />;
}

export function RedirectIfAuthenticated(): React.JSX.Element {
  const status = useSessionStore((s) => s.status);
  if (status === "authenticated") return <Navigate to="/" replace />;
  return <Outlet />;
}
