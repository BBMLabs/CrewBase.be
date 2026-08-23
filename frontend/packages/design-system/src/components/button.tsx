import { forwardRef, type ButtonHTMLAttributes, type ReactNode } from "react";

export type ButtonVariant = "primary" | "secondary" | "ghost" | "danger";
export type ButtonSize = "sm" | "md";

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  readonly variant?: ButtonVariant;
  readonly size?: ButtonSize;
  readonly pending?: boolean;
  readonly iconStart?: ReactNode;
}

const VARIANT_CLASSES: Record<ButtonVariant, string> = {
  primary: [
    "text-white border border-brand-700/60",
    "bg-linear-to-b from-brand-500 to-brand-600",
    "shadow-[inset_0_1px_0_rgb(255_255_255/0.28),var(--shadow-xs)]",
    "hover:shadow-[inset_0_1px_0_rgb(255_255_255/0.3),var(--shadow-sm)] hover:brightness-[1.07]",
    "active:brightness-95 active:translate-y-px active:shadow-none",
  ].join(" "),
  secondary: [
    "bg-surface text-ink border border-line-strong/80",
    "hover:border-line-strong hover:bg-surface-2/70 active:translate-y-px",
    "shadow-xs",
  ].join(" "),
  ghost: [
    "bg-transparent text-brand-700 border border-transparent",
    "hover:bg-brand-50 active:bg-brand-100/70",
  ].join(" "),
  danger: [
    "text-white border border-danger/80",
    "bg-linear-to-b from-[#c94339] to-danger",
    "shadow-[inset_0_1px_0_rgb(255_255_255/0.22),var(--shadow-xs)]",
    "hover:shadow-[inset_0_1px_0_rgb(255_255_255/0.25),var(--shadow-sm)] hover:brightness-[1.05]",
    "active:brightness-95 active:translate-y-px",
  ].join(" "),
};

const SIZE_CLASSES: Record<ButtonSize, string> = {
  sm: "h-8 px-3 text-[13px] gap-1.5 rounded-sm font-semibold tracking-[-0.01em]",
  md: "h-10 px-4 text-sm gap-2 rounded-md font-semibold tracking-[-0.01em]",
};

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(function Button(
  { variant = "primary", size = "md", pending = false, iconStart, className, children, disabled, ...rest },
  ref,
) {
  return (
    <button
      ref={ref}
      type={rest.type ?? "button"}
      disabled={disabled === true || pending}
      aria-busy={pending || undefined}
      className={[
        "inline-flex select-none items-center justify-center whitespace-nowrap",
        "transition-all duration-150 ease-out cursor-pointer",
        "disabled:pointer-events-none disabled:opacity-45 disabled:shadow-none disabled:translate-y-0",
        SIZE_CLASSES[size],
        VARIANT_CLASSES[variant],
        className,
      ]
        .filter(Boolean)
        .join(" ")}
      {...rest}
    >
      {pending ? <Spinner /> : iconStart}
      {children}
    </button>
  );
});

function Spinner(): ReactNode {
  return (
    <span
      aria-hidden="true"
      className="size-4 shrink-0 animate-spin rounded-full border-2 border-current border-t-transparent opacity-80"
    />
  );
}
