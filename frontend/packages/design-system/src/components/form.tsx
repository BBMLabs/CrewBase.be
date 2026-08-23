import { forwardRef, useId, type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes, type TextareaHTMLAttributes } from "react";

const CONTROL_BASE =
  "w-full rounded-md border bg-surface px-3 text-sm text-ink placeholder:text-ink-3/80 " +
  "transition-all duration-150 disabled:cursor-not-allowed disabled:opacity-60 " +
  "shadow-xs border-line hover:border-line-strong " +
  "focus:border-brand-400 focus-visible:outline-none focus:ring-4 focus:ring-brand-500/12 " +
  "aria-[invalid=true]:border-danger aria-[invalid=true]:ring-danger/15";

export interface FormFieldProps {
  readonly label: string;
  readonly htmlFor?: string;
  readonly error?: string | readonly string[];
  readonly hint?: string;
  readonly required?: boolean;
  readonly children: ReactNode | ((id: string) => ReactNode);
  readonly className?: string;
}

export function FormField({ label, htmlFor, error, hint, required, children, className }: FormFieldProps) {
  const autoId = useId();
  const id = htmlFor ?? autoId;
  const errorText = normalizeError(error);
  return (
    <div className={["flex flex-col gap-1.5", className].filter(Boolean).join(" ")}>
      <label htmlFor={id} className="text-[13px] font-medium text-ink-2">
        {label}
        {required === true ? (
          <span aria-hidden="true" className="ml-0.5 text-danger">
            *
          </span>
        ) : null}
      </label>
      {typeof children === "function" ? children(id) : children}
      {hint !== undefined && errorText === null ? (
        <p id={`${id}-hint`} className="text-xs text-ink-3">
          {hint}
        </p>
      ) : null}
      {errorText !== null ? (
        <p id={`${id}-error`} role="alert" className="text-xs font-medium text-danger">
          {errorText}
        </p>
      ) : null}
    </div>
  );
}

export function fieldAria(error: string | readonly string[] | undefined): {
  readonly "aria-invalid": boolean;
  readonly "aria-describedby": string | undefined;
} {
  const has = normalizeError(error) !== null;
  return { "aria-invalid": has, "aria-describedby": undefined };
}

export const Input = forwardRef<HTMLInputElement, InputHTMLAttributes<HTMLInputElement> & ExtraControlProps>(
  function Input({ invalid, className, ...rest }, ref) {
    return (
      <input
        ref={ref}
        aria-invalid={invalid === true || undefined}
        className={[CONTROL_BASE, "h-10", invalid === true ? "border-danger" : "", className]
          .filter(Boolean)
          .join(" ")}
        {...rest}
      />
    );
  },
);

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaHTMLAttributes<HTMLTextAreaElement> & ExtraControlProps>(
  function Textarea({ invalid, className, rows = 4, ...rest }, ref) {
    return (
      <textarea
        ref={ref}
        rows={rows}
        aria-invalid={invalid === true || undefined}
        className={[CONTROL_BASE, "py-2", invalid === true ? "border-danger" : "", className]
          .filter(Boolean)
          .join(" ")}
        {...rest}
      />
    );
  },
);

export const Select = forwardRef<HTMLSelectElement, SelectHTMLAttributes<HTMLSelectElement> & ExtraControlProps>(
  function Select({ invalid, className, children, ...rest }, ref) {
    return (
      <select
        ref={ref}
        aria-invalid={invalid === true || undefined}
        className={[
          CONTROL_BASE,
          "h-10 appearance-none bg-[url('data:image/svg+xml;charset=utf-8,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 width=%2216%22 height=%2216%22 fill=%22none%22%3E%3Cpath d=%22M4 6l4 4 4-4%22 stroke=%22%2351626f%22 stroke-width=%221.5%22/%3E%3C/svg%3E')] bg-[position:right_0.75rem_center] bg-no-repeat pr-9",
          invalid === true ? "border-danger" : "",
          className,
        ]
          .filter(Boolean)
          .join(" ")}
        {...rest}
      >
        {children}
      </select>
    );
  },
);

export interface CheckboxProps extends InputHTMLAttributes<HTMLInputElement> {
  readonly label: ReactNode;
}

export function Checkbox({ label, className, ...rest }: CheckboxProps) {
  const id = useId();
  return (
    <div className={["flex items-start gap-2", className].filter(Boolean).join(" ")}>
      <input
        id={id}
        type="checkbox"
        className="mt-0.5 size-4 rounded-sm border border-line-strong accent-brand-600"
        {...rest}
      />
      <label htmlFor={id} className="text-sm leading-5 text-ink">
        {label}
      </label>
    </div>
  );
}

interface ExtraControlProps {
  readonly invalid?: boolean;
}

export function normalizeError(error: string | readonly string[] | undefined): string | null {
  if (error === undefined) return null;
  if (typeof error === "string") return error === "" ? null : error;
  return error.length > 0 ? (error[0] ?? null) : null;
}
