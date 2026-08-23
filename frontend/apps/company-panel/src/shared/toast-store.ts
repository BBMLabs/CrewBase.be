import { create } from "zustand";

export type ToastTone = "success" | "error" | "info";

export interface ToastItem {
  readonly id: number;
  readonly tone: ToastTone;
  readonly message: string;
  /** Optional traceId for persistent errors (support handoff). */
  readonly traceId?: string;
}

interface ToastState {
  readonly items: readonly ToastItem[];
  push: (tone: ToastTone, message: string, traceId?: string) => void;
  dismiss: (id: number) => void;
}

let nextId = 1;

export const useToastStore = create<ToastState>()((set) => ({
  items: [],
  push: (tone, message, traceId) =>
    set((state) => ({
      items: [...state.items.slice(-3), { id: nextId++, tone, message, traceId }],
    })),
  dismiss: (id) => set((state) => ({ items: state.items.filter((t) => t.id !== id) })),
}));

export function toastSuccess(message: string): void {
  useToastStore.getState().push("success", message);
}
export function toastError(message: string, traceId?: string): void {
  useToastStore.getState().push("error", message, traceId);
}
