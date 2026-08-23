import { useEffect, useRef, useState, type ReactNode } from "react";
import { Input } from "./form";

/* ── CrewAvatar ─────────────────────────────────────────── */

const AVATAR_TONES = [
  "bg-brand-100 text-brand-700",
  "bg-oar-100 text-oar-700",
  "bg-success-bg text-success",
  "bg-info-bg text-info",
  "bg-warning-bg text-warning",
] as const;

function initialsOf(name: string): string {
  const parts = name.trim().split(/\s+/);
  if (parts.length === 0 || parts[0] === "") return "··";
  const first = parts[0]?.[0] ?? "";
  const last = parts.length > 1 ? (parts[parts.length - 1]?.[0] ?? "") : "";
  return `${first}${last}`.toUpperCase();
}

function toneFor(name: string): string {
  let hash = 0;
  for (let i = 0; i < name.length; i += 1) hash = (hash * 31 + name.charCodeAt(i)) % 997;
  return AVATAR_TONES[hash % AVATAR_TONES.length] ?? AVATAR_TONES[0]!;
}

/** Initials avatar for crew members; stable color per name. */
export function CrewAvatar({
  name,
  size = "md",
  className,
}: {
  readonly name: string;
  readonly size?: "sm" | "md";
  readonly className?: string;
}) {
  const dimension = size === "sm" ? "size-6 text-[10px]" : "size-8 text-[12px]";
  return (
    <span
      aria-hidden="true"
      title={name}
      className={[
        "inline-flex shrink-0 items-center justify-center rounded-full font-bold",
        dimension,
        toneFor(name),
        className,
      ]
        .filter(Boolean)
        .join(" ")}
    >
      {initialsOf(name)}
    </span>
  );
}

/* ── MeterBar ───────────────────────────────────────────── */

/** Animated capacity meter (fills on mount, width-transition on change). */
export function MeterBar({
  value,
  max,
  label,
  className,
}: {
  readonly value: number;
  readonly max: number;
  readonly label?: string;
  readonly className?: string;
}) {
  const [mounted, setMounted] = useState(false);
  useEffect(() => setMounted(true), []);
  const pct = Math.min(100, Math.round((value / Math.max(1, max)) * 100));
  const exhausted = value >= max;
  return (
    <div
      role="progressbar"
      aria-valuenow={value}
      aria-valuemin={0}
      aria-valuemax={max}
      aria-label={label ?? `Kullanım ${value}/${max}`}
      className={["h-1.5 w-full overflow-hidden rounded-full bg-surface-2", className].filter(Boolean).join(" ")}
    >
      <div
        style={{ width: mounted ? `${pct}%` : "0%" }}
        className={`h-full rounded-full transition-[width] duration-500 ease-out ${exhausted ? "bg-danger" : "bg-brand-500"}`}
      />
    </div>
  );
}

/* ── FilterBar ──────────────────────────────────────────── */

/**
 * Toolbar pattern for list screens: leading search, trailing custom filters/actions.
 * Layout only — state stays in the page.
 */
export function FilterBar({
  search,
  onSearchChange,
  searchPlaceholder = "Ara…",
  searchLabel = "Ara",
  children,
}: {
  readonly search?: string;
  readonly onSearchChange?: (value: string) => void;
  readonly searchPlaceholder?: string;
  readonly searchLabel?: string;
  /** Custom filters / actions rendered at the end. */
  readonly children?: ReactNode;
}) {
  if (search === undefined || onSearchChange === undefined) {
    return (
      <div className="flex flex-wrap items-center justify-end gap-2">{children}</div>
    );
  }
  return (
    <div className="glass-panel flex flex-wrap items-center justify-between gap-3 rounded-2xl px-3 py-2.5">
      <Input
        type="search"
        value={search}
        onChange={(e) => onSearchChange(e.target.value)}
        placeholder={searchPlaceholder}
        aria-label={searchLabel}
        className="max-w-xs border-0 bg-transparent shadow-none focus:ring-0"
      />
      {children !== undefined ? <div className="flex flex-wrap items-center gap-2">{children}</div> : null}
    </div>
  );
}

/* ── StickyActionBar ────────────────────────────────────── */

/** Bottom sheet-style action bar that slides up while `visible` (dirty forms etc.). */
export function StickyActionBar({
  visible,
  children,
}: {
  readonly visible: boolean;
  readonly children: ReactNode;
}) {
  const previouslyVisible = useRef(false);
  const [render, setRender] = useState(visible);
  // Keep mounted during exit animation.
  useEffect(() => {
    if (visible) {
      setRender(true);
      previouslyVisible.current = true;
      return;
    }
    if (!previouslyVisible.current) return;
    const timer = setTimeout(() => setRender(false), 240);
    return () => clearTimeout(timer);
  }, [visible]);

  if (!render) return null;

  return (
    <div
      role="toolbar"
      aria-label="Kaydetme eylemleri"
      className={[
        "fixed inset-x-0 bottom-0 z-[950] border-t border-line bg-surface/95 backdrop-blur",
        "transition-transform duration-200 ease-out",
        visible ? "translate-y-0" : "translate-y-full",
      ].join(" ")}
    >
      <div className="mx-auto flex max-w-[1240px] items-center justify-end gap-2 px-4 py-3 md:px-8">
        {children}
      </div>
    </div>
  );
}

/* ── TimelineList ───────────────────────────────────────── */

/**
 * Seyir-defteri rail list: left rule with colored knot per row.
 * Rows are plain children; wrap content yourself inside each li.
 */
export function TimelineList({ children, className }: { readonly children: ReactNode; readonly className?: string }) {
  return (
    <ul className={["cb-stagger relative ml-1 flex flex-col divide-y divide-line/60 border-l border-line/80", className].filter(Boolean).join(" ")}>
      {children}
    </ul>
  );
}

export function TimelineRow({
  dotTone = "neutral",
  children,
  className,
}: {
  /** neutral | brand | success | warning | danger */
  readonly dotTone?: "neutral" | "brand" | "success" | "warning" | "danger";
  readonly children: ReactNode;
  readonly className?: string;
}) {
  const dots: Record<string, string> = {
    neutral: "bg-ink-3",
    brand: "bg-brand-400",
    success: "bg-success",
    warning: "bg-warning",
    danger: "bg-danger/70",
  };
  return (
    <li className={["relative py-3 pl-5 pr-4 transition-colors hover:bg-surface-2/40", className].filter(Boolean).join(" ")}>
      <span aria-hidden="true" className={`absolute -left-[5px] top-1/2 size-[9px] -translate-y-1/2 rounded-full ring-4 ring-surface ${dots[dotTone]}`} />
      {children}
    </li>
  );
}
