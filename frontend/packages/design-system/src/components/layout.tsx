import type { ReactNode } from "react";

export function PageHeader({
  title,
  description,
  actions,
  breadcrumb,
  meta,
}: {
  readonly title: string;
  readonly description?: string;
  readonly actions?: ReactNode;
  readonly breadcrumb?: ReactNode;
  /** Small meta row under description (dates, counts). */
  readonly meta?: ReactNode;
}) {
  return (
    <header className="rounded-2xl border border-line/70 bg-surface/85 px-5 py-4 shadow-[0_18px_45px_-28px_rgba(15,29,43,0.45)] backdrop-blur-sm md:px-6 md:py-5">
      <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
        <div className="min-w-0">
          {breadcrumb !== undefined ? (
            <div className="mb-1.5 text-xs font-semibold uppercase tracking-[0.12em] text-ink-3">{breadcrumb}</div>
          ) : null}
          <h1 className="page-title font-display text-[27px] font-extrabold leading-tight tracking-[-0.03em] text-ink md:text-[30px]">
            {title}
          </h1>
          <div className="mt-1.5 flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-ink-2">
            {description !== undefined ? <span>{description}</span> : null}
            {meta !== undefined ? <span className="text-ink-3">{meta}</span> : null}
          </div>
        </div>
        {actions !== undefined ? (
          <div className="flex shrink-0 items-center gap-2 pt-1">{actions}</div>
        ) : null}
      </div>
    </header>
  );
}

export function SectionCard({
  title,
  description,
  actions,
  children,
  className,
  flush = false,
}: {
  readonly title?: string;
  readonly description?: string;
  readonly actions?: ReactNode;
  readonly children: ReactNode;
  readonly className?: string;
  /** Remove inner padding (for tables that manage their own gutters). */
  readonly flush?: boolean;
}) {
  return (
    <section
      className={[
        "overflow-hidden rounded-2xl border border-line/80 bg-surface shadow-[0_16px_38px_-28px_rgba(15,29,43,0.5)]",
        "transition-all duration-200 hover:-translate-y-0.5 hover:shadow-[0_20px_44px_-24px_rgba(15,29,43,0.52)]",
        className,
      ]
        .filter(Boolean)
        .join(" ")}
    >
      {title !== undefined || actions !== undefined ? (
        <div className="flex items-center justify-between gap-3 border-b border-line/70 bg-surface-2/30 px-5 py-3.5">
          <div className="flex flex-col gap-0.5">
            <h2 className="text-sm font-semibold tracking-[-0.01em] text-ink">{title}</h2>
            {description !== undefined ? <p className="text-xs text-ink-3">{description}</p> : null}
          </div>
          {actions !== undefined ? <div className="flex items-center gap-2">{actions}</div> : null}
        </div>
      ) : null}
      <div className={flush ? "" : "px-5 py-4"}>{children}</div>
    </section>
  );
}
