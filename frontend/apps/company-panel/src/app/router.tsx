import { createBrowserRouter, Navigate } from "react-router";
import { AppShell } from "../features/shell/app-shell";
import { LoginPage } from "../features/auth/login-page";
import {
  ForgotPasswordPage,
  ResetPasswordPage,
  SignupPage,
  VerifyEmailPage,
} from "../features/auth/account-pages";
import { RedirectIfAuthenticated, RequireAuth } from "../features/auth/guards";
import { DashboardPage } from "../features/dashboard/dashboard-page";
import { DayProgramPage } from "../features/schedule/day-program-page";
import { MembersPage } from "../features/members/members-page";
import { PackagesPage } from "../features/packages/packages-page";
import { ResourcesPage } from "../features/resources/resources-page";
import { FeedModerationPage } from "../features/feed/feed-moderation-page";
import { UsersPage } from "../features/users/users-page";
import { SettingsPage } from "../features/settings/settings-page";

export function createRouter(): ReturnType<typeof createBrowserRouter> {
  return createBrowserRouter([
    {
      element: <RedirectIfAuthenticated />,
      children: [
        { path: "/giris", element: <LoginPage /> },
        { path: "/kayit", element: <SignupPage /> },
        { path: "/sifremi-unuttum", element: <ForgotPasswordPage /> },
        { path: "/sifre-sifirla", element: <ResetPasswordPage /> },
        { path: "/eposta-dogrula", element: <VerifyEmailPage /> },
      ],
    },
    {
      element: <RequireAuth />,
      children: [
        {
          element: <AppShell />,
          children: [
            { path: "/", element: <DashboardPage /> },
            { path: "/program", element: <DayProgramPage /> },
            { path: "/uyeler", element: <MembersPage /> },
            { path: "/paketler", element: <PackagesPage /> },
            { path: "/kaynaklar", element: <ResourcesPage /> },
            { path: "/akis", element: <FeedModerationPage /> },
            { path: "/kullanicilar", element: <UsersPage /> },
            { path: "/ayarlar", element: <SettingsPage /> },
            { path: "*", element: <Navigate to="/" replace /> },
          ],
        },
      ],
    },
  ]);
}
