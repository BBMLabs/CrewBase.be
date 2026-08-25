import { createBrowserRouter, Navigate } from "react-router";
import { RedirectIfAuthenticated, RequireMember } from "../features/auth/guards";
import { MemberLoginPage, MemberRegisterPage } from "../features/auth/auth-pages";
import { MemberShell } from "../features/shell/member-shell";
import { BookingHomePage } from "../features/home/booking-home-page";
import { MyAppointmentsPage } from "../features/appointments/my-appointments-page";
import { MyPackagesPage } from "../features/packages/my-packages-page";
import { ProfilePage } from "../features/profile/profile-page";
import { ChatThreadPage, FriendsPage } from "../features/chat/friends-page";
import { FeedPage } from "../features/feed/feed-page";

export function createRouter(): ReturnType<typeof createBrowserRouter> {
  return createBrowserRouter([
    {
      element: <RedirectIfAuthenticated />,
      children: [
        { path: "/giris", element: <MemberLoginPage /> },
        { path: "/kayit", element: <MemberRegisterPage /> },
      ],
    },
    {
      element: <RequireMember />,
      children: [
        {
          element: <MemberShell />,
          children: [
            { path: "/", element: <BookingHomePage /> },
            { path: "/randevularim", element: <MyAppointmentsPage /> },
            { path: "/paketlerim", element: <MyPackagesPage /> },
            { path: "/profil", element: <ProfilePage /> },
            { path: "/arkadaslar", element: <FriendsPage /> },
            { path: "/sohbet/:customerId", element: <ChatThreadPage /> },
            { path: "/akis", element: <FeedPage /> },
            { path: "*", element: <Navigate to="/" replace /> },
          ],
        },
      ],
    },
  ]);
}
