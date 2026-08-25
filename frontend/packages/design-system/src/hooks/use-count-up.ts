import { useEffect, useState } from "react";

export interface CountUpOptions {
  readonly durationMs?: number;
  /** Set false to render the target immediately (e.g. reduced motion). */
  readonly enabled?: boolean;
}

function prefersReducedMotion(): boolean {
  return globalThis.matchMedia?.("(prefers-color-scheme: reduce)")?.matches === true ||
    globalThis.matchMedia?.("(prefers-reduced-motion: reduce)")?.matches === true;
}

/**
 * Animates 0 → target with an ease-out cubic using rAF (~650ms default).
 * Falls back to the static value under reduced motion or when disabled.
 */
export function useCountUp(target: number, options: CountUpOptions = {}): number {
  const { durationMs = 650, enabled = true } = options;
  const [value, setValue] = useState(() => (enabled && !prefersReducedMotion() ? 0 : target));

  useEffect(() => {
    if (!enabled || prefersReducedMotion()) {
      setValue(target);
      return;
    }
    let raf = 0;
    const start = performance.now();
    const tick = (now: number) => {
      const progress = Math.min(1, (now - start) / durationMs);
      const eased = 1 - Math.pow(1 - progress, 3);
      setValue(Math.round(target * eased));
      if (progress < 1) raf = requestAnimationFrame(tick);
    };
    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [target, durationMs, enabled]);

  return value;
}
