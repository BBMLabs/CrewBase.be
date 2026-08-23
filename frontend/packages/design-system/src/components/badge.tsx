import type { ReactNode } from "react";

export type BadgeTone = "neutral" | "info" | "success" | "warning" | "danger" | "accent";

const DOT_CLASSES: Record<BadgeTone, string> = {
  neutral: "bg-ink-3",
  info: "bg-brand-500",
  success: "bg-success",
  warning: "bg-warning",
  danger: "bg-danger",
  accent: "bg-oar-600",
};

const TEXT_CLASSES: Record<BadgeTone, string> = {
  neutral: "text-ink-2 border-line bg-surface",
  info: "text-brand-700 border-brand-200 bg-brand-50",
  success: "text-success border-success/25 bg-success-bg",
  warning: "text-warning border-warning/25 bg-warning-bg",
  danger: "text-danger border-danger/25 bg-danger-bg",
  accent: "text-oar-700 border-oar-300/60 bg-oar-100",
};

/**
 * Modern status pill: small dot + tinted text, hairline border.
 * `dot={false}` renders a plain tinted chip (for counts etc.).
 */
export function Badge({
  tone = "neutral",
  dot = true,
  children,
  className,
}: {
  readonly tone?: BadgeTone;
  readonly dot?: boolean;
  readonly children: ReactNode;
  readonly className?: string;
}) {
  return (
    <span
      className={[
        "inline-flex items-center gap-1.5 rounded-full border px-2.5 py-0.5",
        "text-xs font-semibold leading-5 tracking-[-0.01em] whitespace-nowrap",
        TEXT_CLASSES[tone],
        className,
      ]
        .filter(Boolean)
        .join(" ")}
    >
      {dot ? <span aria-hidden="true" className={`size-1.5 rounded-full ${DOT_CLASSES[tone]}`} /> : null}
      {children}
    </span>
  );
}

/** Maps backend AppointmentStatus strings to badge tones (labels from labels/tr). */
const STATUS_TONES: Record<string, BadgeTone> = {
  Pending: "warning",
  Confirmed: "info",
  Completed: "success",
  Cancelled: "danger",
  Active: "success",
  Passive: "neutral",
  Expired: "danger",
};

export function StatusBadge({
  status,
  label,
  className,
}: {
  /** Raw backend enum string, e.g. "Pending" — used for tone lookup. */
  readonly status: string;
  /** Pre-localized label. */
  readonly label: string;
  readonly className?: string;
}) {
  return (
    <Badge tone={STATUS_TONES[status] ?? "neutral"} className={className}>
      {label}
    </Badge>
  );
}
