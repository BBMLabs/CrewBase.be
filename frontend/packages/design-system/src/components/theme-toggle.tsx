import { useTheme } from "../theming/use-theme";

/**
 * Sun/moon icon button that flips `<html data-theme>`; persists via useTheme.
 * `tone="onDark"` keeps legible contrast when placed on an always-dark surface
 * (sidebar rail, hero bands) regardless of the active theme.
 */
export function ThemeToggle({
  className,
  tone = "auto",
}: {
  readonly className?: string;
  readonly tone?: "auto" | "onDark";
}) {
  const { theme, toggle } = useTheme();
  const isDark = theme === "dark";

  const toneClasses =
    tone === "onDark"
      ? "text-rail-text hover:bg-white/10 hover:text-white"
      : isDark
        ? "text-oar-300 hover:bg-white/10"
        : "text-ink-2 hover:bg-surface-2";

  return (
    <button
      type="button"
      onClick={toggle}
      aria-label={isDark ? "Açık temaya geç" : "Koyu temaya geç"}
      title={isDark ? "Açık tema" : "Koyu tema"}
      className={[
        "inline-flex size-9 items-center justify-center rounded-md transition-colors duration-150",
        "border border-transparent cursor-pointer",
        toneClasses,
        className,
      ]
        .filter(Boolean)
        .join(" ")}
    >
      {isDark ? (
        // Moon
        <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
          <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z" />
        </svg>
      ) : (
        // Sun
        <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
          <circle cx="12" cy="12" r="4" />
          <path d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41" />
        </svg>
      )}
    </button>
  );
}
