import type { ReactNode } from "react";

/**
 * Split-screen auth layout.
 *
 * Brand rail tells the product story in rowing language and carries a calm,
 * CSS/SVG-only water scene (two drifting wave layers + a gently bobbing boat).
 * Everything animates compositor-friendly properties (transform/opacity) and
 * respects prefers-reduced-motion via the global overrides in theme.css.
 *
 * Mobile collapses to a compact branded top strip so the form stays above the fold.
 */

function WaveLayer({ className }: { readonly className?: string }) {
  return (
    <svg
      className={`h-20 w-1/2 shrink-0 ${className ?? ""}`}
      viewBox="0 0 600 80"
      fill="none"
      preserveAspectRatio="none"
      aria-hidden="true"
    >
      <path
        d="M0 40 C 75 18, 150 62, 225 40 S 375 18, 450 40 S 525 62, 600 40 L 600 80 L 0 80 Z"
        fill="currentColor"
      />
      <path d="M0 40 C 75 18, 150 62, 225 40 S 375 18, 450 40 S 525 62, 600 40" stroke="currentColor" strokeWidth="1.4" />
    </svg>
  );
}

/** Calm water scene: two parallax wave bands + a small drifting boat. Pure SVG/CSS. */
function WaterScene() {
  return (
    <div aria-hidden="true" className="pointer-events-none absolute inset-x-0 bottom-0 h-48 select-none">
      {/* Boat */}
      <div className="boat-bob absolute bottom-28 left-[16%] text-white/85">
        <svg width="120" height="46" viewBox="0 0 120 46" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
          {/* hull */}
          <path d="M14 30 Q60 42 106 30 L98 38 Q60 46 22 38 Z" fill="currentColor" stroke="none" />
          {/* riggers / oars */}
          <path d="M34 29 L20 12M86 29 L100 12" strokeWidth="1.8" />
          <circle cx="19" cy="10.5" r="2.4" fill="currentColor" stroke="none" />
          <circle cx="101" cy="10.5" r="2.4" fill="currentColor" stroke="none" />
          {/* crew dots */}
          <circle cx="48" cy="27" r="2.2" fill="currentColor" stroke="none" />
          <circle cx="60" cy="27" r="2.2" fill="currentColor" stroke="none" />
          <circle cx="72" cy="27" r="2.2" fill="currentColor" stroke="none" />
        </svg>
      </div>

      {/* Back wave band (slower, fainter) */}
      <div className="absolute inset-x-0 bottom-10 flex w-[200%] text-brand-300/25 wave-drift" style={{ ["--wave-duration" as string]: "26s" }}>
        <WaveLayer />
        <WaveLayer />
      </div>
      {/* Front wave band */}
      <div className="absolute inset-x-0 bottom-0 flex w-[200%] text-brand-400/35 wave-drift" style={{ ["--wave-duration" as string]: "17s" }}>
        <WaveLayer />
        <WaveLayer />
      </div>
    </div>
  );
}

const BENEFITS: readonly string[] = [
  "Randevular ve seanslar tek ekrandan yönetilir",
  "Seviyeye göre kürek eşleştirmesi otomatik yapılır",
  "Paket bakiyeleri ve hatırlatmalar kendiliğinden yürür",
];

function BrandMark({ size = 40 }: { readonly size?: number }) {
  return (
    <span
      aria-hidden="true"
      className="flex items-center justify-center rounded-xl bg-linear-to-br from-brand-400 to-brand-600 shadow-md"
      style={{ width: size, height: size }}
    >
      <svg width={size * 0.55} height={size * 0.55} viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <path
          d="M3 16c2.2 0 2.2-1.8 4.5-1.8S9.7 16 12 16s2.3-1.8 4.5-1.8S19.8 16 21 16M6 11l6-7 6 7"
          stroke="#fff"
          strokeWidth="1.9"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
    </span>
  );
}

export function AuthLayout({ children }: { readonly children: ReactNode }) {
  return (
    <div className="flex min-h-dvh flex-col lg:flex-row">
      {/* ── Mobile brand strip ─────────────────────────────── */}
      <div className="relative overflow-hidden bg-rail pb-10 pt-5 lg:hidden">
        <div className="relative z-10 mx-auto flex max-w-md items-center gap-2.5 px-5">
          <BrandMark size={32} />
          <span className="font-display text-base font-extrabold tracking-[-0.01em] text-white">CrewBase</span>
          <span className="ml-auto text-[11px] font-semibold uppercase tracking-[0.14em] text-rail-text">Kürek kulübü</span>
        </div>
        <p className="relative z-10 mx-auto mt-2 max-w-md px-5 font-display text-[15px] font-bold leading-snug text-white/90">
          Kulübün su üstündeki komuta merkezi.
        </p>
        <div className="pointer-events-none absolute inset-x-0 bottom-0 flex w-[200%] text-brand-300/30 wave-drift" style={{ ["--wave-duration" as string]: "20s" }}>
          <svg className="h-8 w-1/2 shrink-0" viewBox="0 0 600 40" preserveAspectRatio="none" fill="none" aria-hidden="true">
            <path d="M0 18 C 90 4, 180 30, 300 18 S 480 4, 600 18 L 600 40 L 0 40 Z" fill="currentColor" />
          </svg>
          <svg className="h-8 w-1/2 shrink-0" viewBox="0 0 600 40" preserveAspectRatio="none" fill="none" aria-hidden="true">
            <path d="M0 18 C 90 4, 180 30, 300 18 S 480 4, 600 18 L 600 40 L 0 40 Z" fill="currentColor" />
          </svg>
        </div>
      </div>

      {/* ── Desktop brand rail ─────────────────────────────── */}
      <aside className="relative hidden w-[44%] max-w-[600px] flex-col justify-between overflow-hidden bg-rail p-10 lg:flex xl:p-12">
        {/* Ambient glows */}
        <div
          aria-hidden="true"
          className="pointer-events-none absolute inset-0"
          style={{
            background:
              "radial-gradient(52rem 30rem at 118% -12%, rgb(64 144 196 / 0.32) 0%, transparent 58%)," +
              "radial-gradient(40rem 26rem at -22% 112%, rgb(189 102 49 / 0.18) 0%, transparent 55%)",
          }}
        />

        <div className="cb-rise relative flex items-center gap-3">
          <BrandMark />
          <div className="leading-tight">
            <p className="font-display text-lg font-extrabold tracking-[-0.01em] text-white">CrewBase</p>
            <p className="text-[11px] font-semibold uppercase tracking-[0.14em] text-rail-text">Kürek kulübü platformu</p>
          </div>
        </div>

        <div className="relative max-w-md pb-24">
          <h2
            className="cb-rise font-display text-[30px] font-extrabold leading-[1.25] tracking-[-0.02em] text-white xl:text-[34px]"
            style={{ animationDelay: "70ms" }}
          >
            Kürek sadece bir spor değil.
            <br />
            <span className="text-oar-300">Birlikte hareket etmektir.</span>
          </h2>
          <p
            className="cb-rise mt-4 text-[15px] leading-relaxed text-rail-text"
            style={{ animationDelay: "140ms" }}
          >
            Antrenmanlar, üyeler ve kulüp hayatı — CrewBase ile hepsi tek bir yerde.
          </p>

          <ul className="mt-8 space-y-3.5">
            {BENEFITS.map((benefit, index) => (
              <li
                key={benefit}
                className="cb-rise flex items-start gap-3 text-sm leading-relaxed text-white/85"
                style={{ animationDelay: `${200 + index * 70}ms` }}
              >
                <span
                  aria-hidden="true"
                  className="mt-0.5 flex size-5 shrink-0 items-center justify-center rounded-full bg-success/25 text-[11px] font-bold text-emerald-200"
                >
                  ✓
                </span>
                {benefit}
              </li>
            ))}
          </ul>
        </div>

        <WaterScene />

        <p className="relative text-xs text-rail-text/60">© {new Date().getFullYear()} CrewBase</p>
      </aside>

      {/* ── Form side ──────────────────────────────────────── */}
      <main className="flex flex-1 items-start justify-center bg-canvas px-5 pb-10 pt-10 sm:px-8 lg:items-center lg:pt-0">
        <div className="cb-rise w-full max-w-[430px]" style={{ animationDelay: "110ms" }}>
          {children}
        </div>
      </main>
    </div>
  );
}

export function AuthCard({
  title,
  subtitle,
  children,
}: {
  readonly title: string;
  readonly subtitle?: string;
  readonly children: ReactNode;
}) {
  return (
    <section className="rounded-xl border border-line/70 bg-surface p-6 shadow-md sm:p-8">
      <h1 className="font-display text-[22px] font-extrabold tracking-[-0.02em] text-ink">{title}</h1>
      {subtitle !== undefined ? <p className="mt-1.5 text-sm leading-relaxed text-ink-2">{subtitle}</p> : null}
      <div className="mt-6">{children}</div>
    </section>
  );
}
