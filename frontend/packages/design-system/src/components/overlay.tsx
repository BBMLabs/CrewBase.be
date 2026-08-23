import { useCallback, useEffect, useRef, type ReactNode } from "react";
import { Button } from "./button";

const FOCUSABLE =
  'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])';

function useOverlay(open: boolean, onClose: () => void) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const previouslyFocused = useRef<HTMLElement | null>(null);

  useEffect(() => {
    if (!open) return;
    previouslyFocused.current = document.activeElement as HTMLElement | null;

    const container = containerRef.current;
    if (container !== null) {
      const first = container.querySelector<HTMLElement>(FOCUSABLE);
      (first ?? container).focus();
    }

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        event.stopPropagation();
        onClose();
        return;
      }
      if (event.key !== "Tab" || containerRef.current === null) return;
      const focusables = Array.from(
        containerRef.current.querySelectorAll<HTMLElement>(FOCUSABLE),
      ).filter((el) => el.offsetParent !== null || el === document.activeElement);
      if (focusables.length === 0) return;
      const firstItem = focusables[0]!;
      const lastItem = focusables[focusables.length - 1]!;
      if (event.shiftKey && document.activeElement === firstItem) {
        event.preventDefault();
        lastItem.focus();
      } else if (!event.shiftKey && document.activeElement === lastItem) {
        event.preventDefault();
        firstItem.focus();
      }
    };

    document.addEventListener("keydown", onKeyDown);
    const { overflow } = document.body.style;
    document.body.style.overflow = "hidden";
    return () => {
      document.removeEventListener("keydown", onKeyDown);
      document.body.style.overflow = overflow;
      previouslyFocused.current?.focus?.();
    };
  }, [open, onClose]);

  return containerRef;
}

function Backdrop({ onClose }: { readonly onClose: () => void }) {
  return (
    <div
      className="fixed inset-0 z-[1100] bg-black/55 backdrop-blur-[2px]"
      aria-hidden="true"
      onClick={onClose}
    />
  );
}

export function Dialog({
  open,
  onClose,
  title,
  description,
  children,
  footer,
  size = "md",
}: {
  readonly open: boolean;
  readonly onClose: () => void;
  readonly title: string;
  readonly description?: string;
  readonly children: ReactNode;
  readonly footer?: ReactNode;
  readonly size?: "sm" | "md" | "lg";
}) {
  const containerRef = useOverlay(open, onClose);
  if (!open) return null;

  const widthClass = size === "sm" ? "max-w-sm" : size === "lg" ? "max-w-2xl" : "max-w-lg";

  return (
    <div className="contents">
      <Backdrop onClose={onClose} />
      <div
        role="dialog"
        aria-modal="true"
        aria-label={title}
        ref={containerRef}
        tabIndex={-1}
        className={[
          "fixed left-1/2 top-1/2 z-[1200] w-[calc(100vw-2rem)] -translate-x-1/2 -translate-y-1/2",
          "flex max-h-[85vh] flex-col rounded-lg border border-line/80 bg-surface shadow-lg outline-none",
          widthClass,
        ].join(" ")}
      >
        <div className="border-b border-line px-5 py-4">
          <h2 className="font-display text-base font-semibold text-ink">{title}</h2>
          {description !== undefined ? (
            <p className="mt-0.5 text-sm text-ink-2">{description}</p>
          ) : null}
        </div>
        <div className="overflow-y-auto px-5 py-4">{children}</div>
        {footer !== undefined ? (
          <div className="flex items-center justify-end gap-2 border-t border-line px-5 py-3">
            {footer}
          </div>
        ) : null}
      </div>
    </div>
  );
}

export function Drawer({
  open,
  onClose,
  title,
  description,
  children,
  footer,
}: {
  readonly open: boolean;
  readonly onClose: () => void;
  readonly title: string;
  readonly description?: string;
  readonly children: ReactNode;
  readonly footer?: ReactNode;
}) {
  const containerRef = useOverlay(open, onClose);
  if (!open) return null;

  return (
    <div className="contents">
      <Backdrop onClose={onClose} />
      <aside
        role="dialog"
        aria-modal="true"
        aria-label={title}
        ref={containerRef}
        tabIndex={-1}
        className="fixed inset-y-0 right-0 z-[1100] flex w-full max-w-md flex-col border-l border-line bg-surface shadow-md outline-none"
      >
        <div className="border-b border-line px-5 py-4">
          <div className="flex items-start justify-between gap-3">
            <div>
              <h2 className="font-display text-base font-semibold text-ink">{title}</h2>
              {description !== undefined ? (
                <p className="mt-0.5 text-sm text-ink-2">{description}</p>
              ) : null}
            </div>
            <Button variant="ghost" size="sm" onClick={onClose} aria-label="Kapat">
              ✕
            </Button>
          </div>
        </div>
        <div className="flex-1 overflow-y-auto px-5 py-4">{children}</div>
        {footer !== undefined ? (
          <div className="flex items-center justify-end gap-2 border-t border-line px-5 py-3">
            {footer}
          </div>
        ) : null}
      </aside>
    </div>
  );
}

export function ConfirmDialog({
  open,
  onClose,
  onConfirm,
  title,
  description,
  confirmLabel = "Onayla",
  cancelLabel = "Vazgeç",
  danger = false,
  pending = false,
}: {
  readonly open: boolean;
  readonly onClose: () => void;
  readonly onConfirm: () => void;
  readonly title: string;
  readonly description: string;
  readonly confirmLabel?: string;
  readonly cancelLabel?: string;
  readonly danger?: boolean;
  readonly pending?: boolean;
}) {
  const handleConfirm = useCallback(() => {
    onConfirm();
  }, [onConfirm]);

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={title}
      size="sm"
      footer={
        <>
          <Button variant="secondary" size="sm" onClick={onClose}>
            {cancelLabel}
          </Button>
          <Button
            variant={danger ? "danger" : "primary"}
            size="sm"
            pending={pending}
            onClick={handleConfirm}
          >
            {confirmLabel}
          </Button>
        </>
      }
    >
      <p className="text-sm text-ink-2">{description}</p>
    </Dialog>
  );
}
