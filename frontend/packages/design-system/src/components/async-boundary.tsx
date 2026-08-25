import type { ReactNode } from "react";
import { AnchorArt } from "./nautical";
import { Button, type ButtonProps } from "./button";

/**
 * Structural query-state shape — satisfied by TanStack Query results without
 * coupling the design system to the query library.
 */
export interface AsyncState {
  readonly isPending: boolean;
  readonly isError: boolean;
  readonly error: unknown;
  readonly refetch: () => void;

  /** Optional flags surfaced by callers when known. */
  readonly isPermissionError?: boolean;
  readonly isNotFound?: boolean;
}

export function Skeleton({ className }: { readonly className?: string }) {
  return <div aria-hidden="true" className={`animate-pulse rounded-md bg-surface-2 ${className ?? ""}`} />;
}

export function TableSkeleton({
  rows = 6,
  columns = 4,
}: {
  readonly rows?: number;
  readonly columns?: number;
}) {
  return (
    <div role="status" aria-label="İçerik yükleniyor" className="flex flex-col gap-2">
      {Array.from({ length: rows }, (_, row) => (
        <div key={row} className="grid gap-3" style={{ gridTemplateColumns: `repeat(${columns}, 1fr)` }}>
          {Array.from({ length: columns }, (_, column) => (
            <Skeleton key={column} className="h-5" />
          ))}
        </div>
      ))}
    </div>
  );
}

export function EmptyState({
  icon,
  title,
  description,
  action,
}: {
  readonly icon?: ReactNode;
  readonly title: string;
  readonly description?: string;
  readonly action?: ReactNode;
}) {
  return (
    <div className="flex flex-col items-center justify-center gap-2.5 rounded-lg border border-dashed border-line bg-surface px-6 py-14 text-center">
      {icon !== undefined ? (
        <div className="text-ink-3">{icon}</div>
      ) : (
        <AnchorArt className="mb-1" />
      )}
      <p className="font-display text-base font-bold tracking-[-0.01em] text-ink">{title}</p>
      {description !== undefined ? (
        <p className="max-w-md text-sm leading-relaxed text-ink-2">{description}</p>
      ) : null}
      {action !== undefined ? <div className="mt-2">{action}</div> : null}
    </div>
  );
}

export interface ErrorStateCopy {
  readonly title?: string;
  readonly description?: string;
  readonly traceId?: string;
}

export function ErrorState({
  title = "Bir sorun oluştu",
  description = "İşlem tamamlanamadı. Lütfen tekrar deneyin.",
  detail,
  traceId,
  retryLabel = "Tekrar dene",
  onRetry,
}: {
  readonly title?: string;
  readonly description?: string;
  /** Server-provided message shown as secondary line when present. */
  readonly detail?: string;
  readonly traceId?: string;
  readonly retryLabel?: string;
  readonly onRetry?: () => void;
}) {
  return (
    <div
      role="alert"
      className="flex flex-col items-center justify-center gap-2 rounded-lg border border-danger/30 bg-danger-bg px-6 py-10 text-center"
    >
      <p className="font-display text-base font-semibold text-danger">{title}</p>
      <p className="max-w-md text-sm text-ink-2">{description}</p>
      {detail !== undefined && detail !== description ? (
        <p className="max-w-md text-xs text-ink-3">{detail}</p>
      ) : null}
      {traceId !== undefined ? (
        <p className="font-mono text-[11px] text-ink-3">İzleme no: {traceId}</p>
      ) : null}
      {onRetry !== undefined ? (
        <Button size="sm" variant="secondary" onClick={onRetry} className="mt-2">
          {retryLabel}
        </Button>
      ) : null}
    </div>
  );
}

function describeError(error: unknown): { code: string | undefined; message: string; traceId: string | undefined } {
  if (error instanceof Error) {
    const candidate = error as Error & { code?: string; traceId?: string; serverMessage?: string };
    return {
      code: candidate.code,
      message: candidate.serverMessage ?? candidate.message,
      traceId: candidate.traceId,
    };
  }
  return { code: undefined, message: String(error), traceId: undefined };
}

const NOT_FOUND_CODES = new Set(["company_not_found"]);
const PERMISSION_CODES = new Set(["unauthorized", "forbidden"]);

/**
 * Renders loading / error / permission-denied / not-found / empty / success states.
 * `isEmpty` lets list screens declare their own empty criterion.
 */
export function AsyncBoundary({
  state,
  isEmpty,
  emptyState,
  children,
}: {
  readonly state: AsyncState;
  readonly isEmpty?: boolean;
  readonly emptyState?: ReactNode;
  readonly children: ReactNode;
}) {
  if (state.isPending) {
    return (
      <div className="py-6">
        <TableSkeleton />
      </div>
    );
  }

  if (state.isError) {
    const { code, message, traceId } = describeError(state.error);
    if (code !== undefined && PERMISSION_CODES.has(code)) {
      return (
        <EmptyState
          title="Erişim yetkiniz yok"
          description="Bu alanı görüntülemek için gerekli yetkiye sahip değilsiniz."
        />
      );
    }
    if (code !== undefined && NOT_FOUND_CODES.has(code)) {
      return (
        <EmptyState
          title="Kulüp bulunamadı"
          description="Hesabınıza bağlı aktif bir kulüp bulunamadı. Lütfen yöneticinizle iletişime geçin."
        />
      );
    }
    return (
      <ErrorState
        description={message}
        traceId={traceId}
        onRetry={state.isError ? () => state.refetch() : undefined}
      />
    );
  }

  if (isEmpty === true && emptyState !== undefined) {
    return <>{emptyState}</>;
  }

  return <>{children}</>;
}

export type { ButtonProps };
