/**
 * Nautical identity pieces — pronounced but calm.
 * All decorative; every component is aria-hidden and purely presentational.
 */

/** Hairline wave rule used between major sections. Static by design. */
export function WaveDivider({ className }: { readonly className?: string }) {
  return (
    <div aria-hidden="true" className={`overflow-hidden ${className ?? "h-6"}`}>
      <svg
        className="h-full w-full text-line-strong/50"
        viewBox="0 0 1200 24"
        preserveAspectRatio="none"
        fill="none"
      >
        <path
          d="M0 12 C 150 2, 300 22, 450 12 S 750 2, 900 12 s 225 10, 300 0"
          stroke="currentColor"
          strokeWidth="1.5"
        />
        <path
          d="M0 18 C 150 10, 300 26, 450 18 S 750 10, 900 18 s 225 8, 300 0"
          stroke="currentColor"
          strokeWidth="1"
          opacity="0.5"
        />
      </svg>
    </div>
  );
}

/** Pennant bunting strip: alternating signal-flag triangles with a gentle sway. */
export function PennantStrip({ count = 16, className }: { readonly count?: number; readonly className?: string }) {
  const fills = ["fill-brand-400/80", "fill-oar-500/70", "fill-success/60", "fill-brand-200/70"];
  const flags = Array.from({ length: count }, (_, index) => {
    const x = index * 46;
    const fill = fills[index % fills.length];
    return <path key={index} d={`M${x} 0 L${x + 34} 0 L${x + 17} 26 Z`} className={fill} />;
  });
  return (
    <div aria-hidden="true" className={`pennant-sway overflow-hidden ${className ?? ""}`}>
      <svg height="28" width={count * 46} viewBox={`0 0 ${count * 46} 28`} preserveAspectRatio="xMinYMin meet">
        <line x1="0" y1="1" x2={count * 46} y2="1" stroke="currentColor" strokeWidth="1.4" />
        {flags}
      </svg>
    </div>
  );
}

/** Compass rose watermark — very slow rotation, near-invisible ink. */
export function CompassRose({ className }: { readonly className?: string }) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 100 100"
      className={`compass-spin ${className ?? ""}`}
      fill="none"
      stroke="currentColor"
    >
      <circle cx="50" cy="50" r="44" strokeWidth="1" opacity="0.7" />
      <circle cx="50" cy="50" r="34" strokeWidth="0.6" opacity="0.5" />
      {[0, 45, 90, 135, 180, 225, 270, 315].map((angle) => (
        <line
          key={angle}
          x1="50"
          y1="50"
          x2={50 + 42 * Math.cos((angle * Math.PI) / 180)}
          y2={50 + 42 * Math.sin((angle * Math.PI) / 180)}
          strokeWidth={angle % 90 === 0 ? 1.4 : 0.6}
          opacity={angle % 90 === 0 ? 0.9 : 0.45}
        />
      ))}
      {/* Cardinal points */}
      <path d="M50 6 L54 20 L46 20 Z" fill="currentColor" stroke="none" opacity="0.95" />
      <path d="M50 94 L54 80 L46 80 Z" fill="currentColor" stroke="none" opacity="0.6" />
      <path d="M6 50 L20 46 L20 54 Z" fill="currentColor" stroke="none" opacity="0.6" />
      <path d="M94 50 L80 46 L80 54 Z" fill="currentColor" stroke="none" opacity="0.6" />
    </svg>
  );
}

/** Soft anchor medallion for empty states (default art when no icon given). */
export function AnchorArt({ className }: { readonly className?: string }) {
  return (
    <span
      aria-hidden="true"
      className={[
        "flex size-12 items-center justify-center rounded-full",
        "border border-line bg-surface-2 text-ink-3",
        className ?? "",
      ]
        .filter(Boolean)
        .join(" ")}
    >
      <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">
        <circle cx="12" cy="5" r="2.5" />
        <path d="M12 7.5V21M12 21c-4.5 0-8-3-8.5-7l2.8 1.6M12 21c4.5 0 8-3 8.5-7l-2.8 1.6M7 12h10" />
      </svg>
    </span>
  );
}
