import { useEffect } from "react";
import { useToastStore, type ToastTone } from "./toast-store";

const TONE_CLASSES: Record<ToastTone, string> = {
  success: "border-success/30 bg-success-bg text-success",
  error: "border-danger/30 bg-danger-bg text-danger",
  info: "border-line bg-surface text-ink",
};

export function Toaster() {
  const items = useToastStore((s) => s.items);
  const dismiss = useToastStore((s) => s.dismiss);

  useEffect(() => {
    if (items.length === 0) return;
    const timers = items.map((item) =>
      setTimeout(() => dismiss(item.id), item.tone === "error" ? 8000 : 4000),
    );
    return () => {
      for (const timer of timers) clearTimeout(timer);
    };
  }, [items, dismiss]);

  if (items.length === 0) return null;

  return (
    <div
      aria-live="polite"
      className="pointer-events-none fixed inset-x-0 bottom-20 z-[1300] flex flex-col items-center gap-2 px-4 md:bottom-4"
    >
      {items.map((item) => (
        <div
          key={item.id}
          role={item.tone === "error" ? "alert" : "status"}
          className={[
            "pointer-events-auto flex w-full max-w-md items-start gap-3 rounded-md border px-4 py-3 shadow-md",
            TONE_CLASSES[item.tone],
          ].join(" ")}
        >
          <p className="text-sm font-medium">{item.message}</p>
          <button
            type="button"
            onClick={() => dismiss(item.id)}
            aria-label="Bildirimi kapat"
            className="ml-auto shrink-0 text-xs text-ink-3 hover:text-ink"
          >
            ✕
          </button>
        </div>
      ))}
    </div>
  );
}
