import { useCallback, useEffect, useState } from "react";

export type Theme = "light" | "dark";

const STORAGE_KEY = "crewbase.theme";

function readInitial(): Theme {
  try {
    const stored = globalThis.localStorage?.getItem(STORAGE_KEY);
    if (stored === "light" || stored === "dark") return stored;
  } catch {
    /* storage unavailable */
  }
  // Brand default: the maritime night theme IS the product look.
  // Users can switch to light via the toggle; choice persists.
  return "dark";
}

function apply(theme: Theme): void {
  document.documentElement.dataset["theme"] = theme;
}

/** Theme state bound to `<html data-theme>`, persisted in localStorage.
 *  index.html carries a pre-paint script so the first frame already matches. */
export function useTheme(): {
  readonly theme: Theme;
  readonly setTheme: (theme: Theme) => void;
  readonly toggle: () => void;
} {
  const [theme, setThemeState] = useState<Theme>(readInitial);

  useEffect(() => {
    apply(theme);
  }, [theme]);

  // Follow OS changes only while the user has not made an explicit choice.
  useEffect(() => {
    const media = globalThis.matchMedia?.("(prefers-color-scheme: dark)");
    if (media === undefined) return;
    const onChange = (event: MediaQueryListEvent) => {
      let stored: string | null = null;
      try {
        stored = globalThis.localStorage?.getItem(STORAGE_KEY) ?? null;
      } catch {
        /* ignore */
      }
      if (stored !== "light" && stored !== "dark") {
        setThemeState(event.matches ? "dark" : "light");
      }
    };
    media.addEventListener("change", onChange);
    return () => media.removeEventListener("change", onChange);
  }, []);

  const setTheme = useCallback((next: Theme) => {
    setThemeState(next);
    try {
      globalThis.localStorage?.setItem(STORAGE_KEY, next);
    } catch {
      /* ignore */
    }
  }, []);

  const toggle = useCallback(() => {
    setTheme(theme === "dark" ? "light" : "dark");
  }, [theme, setTheme]);

  return { theme, setTheme, toggle };
}
