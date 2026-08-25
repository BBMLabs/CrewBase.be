import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link } from "react-router";
import type { BookAppointmentResponse } from "@crewbase/api-types";
import {
  AsyncBoundary,
  Badge,
  BOAT_CLASS_LABELS,
  Button,
  EmptyState,
  FormField,
  rowingLevelLabel,
} from "@crewbase/design-system";
import { useApi } from "../../app/api-context";
import { resolveClubSubdomain } from "../../app/club";
import { useSessionStore } from "../auth/session-store";
import { addDaysIso, formatIsoDateTr, todayIso, weekdayShort } from "../../shared/dates";
import { toastError } from "../../shared/toast-store";

/** Answers the member's first question: "Bir sonraki küreğim ne zaman?" */
function NextSessionCard() {
  const api = useApi();
  const appointments = useQuery({
    queryKey: ["member", "appointments"],
    queryFn: () => api.member.appointments(),
    staleTime: 30_000,
  });

  const next = useMemo(() => {
    const today = todayIso();
    return (appointments.data ?? [])
      .filter((a) => a.date >= today && a.status !== "Cancelled" && a.status !== "Completed")
      .sort((a, b) => (a.date + a.startTime).localeCompare(b.date + b.startTime))[0];
  }, [appointments.data]);

  if (appointments.isPending || appointments.isError || next === undefined) return null;

  return (
    <Link
      to="/randevularim"
      className="group relative block overflow-hidden rounded-xl bg-rail p-5 shadow-md transition-transform duration-200 hover:-translate-y-0.5"
    >
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-0"
        style={{ background: "radial-gradient(26rem 16rem at 110% -40%, rgb(64 144 196 / 0.35) 0%, transparent 60%)" }}
      />
      <div className="relative flex items-center justify-between gap-4">
        <div className="min-w-0">
          <p className="text-[11px] font-bold uppercase tracking-[0.09em] text-rail-text">Sonraki küreğin</p>
          <p className="font-display mt-1 text-[22px] font-extrabold leading-tight tracking-[-0.01em] text-white tnum">
            {formatIsoDateTr(next.date)} · {next.startTime}
          </p>
          <p className="mt-1 truncate text-[13px] text-rail-text">
            {next.boatClass}
            {next.boatName !== null ? ` · ${next.boatName}` : ""}
            {next.instructorName !== null ? ` · ${next.instructorName}` : ""}
          </p>
        </div>
        <span
          aria-hidden="true"
          className="flex size-10 shrink-0 items-center justify-center rounded-full bg-white/10 text-white transition-transform duration-200 group-hover:translate-x-1"
        >
          →
        </span>
      </div>
    </Link>
  );
}

export function BookingHomePage() {
  const api = useApi();
  const subdomain = resolveClubSubdomain() ?? "";
  const member = useSessionStore((s) => s.member);
  const queryClient = useQueryClient();

  const [date, setDate] = useState(todayIso());
  const [boatClass, setBoatClass] = useState("1x");
  const [selectedSlot, setSelectedSlot] = useState<string | null>(null);
  const [usePackage, setUsePackage] = useState(false);
  const [reminderMinutes, setReminderMinutes] = useState<number | null>(null);
  const [note, setNote] = useState("");
  const [acceptedConsents, setAcceptedConsents] = useState<string[]>([]);
  const [confirmation, setConfirmation] = useState<BookAppointmentResponse | null>(null);

  const clubReady = subdomain !== "";

  const options = useQuery({
    queryKey: ["public", subdomain, "options"],
    queryFn: () => api.public.options(subdomain),
    enabled: clubReady,
    staleTime: 10 * 60_000,
  });

  const availability = useQuery({
    queryKey: ["public", subdomain, "availability", { date, boatClass }],
    queryFn: () => api.public.availability(subdomain, { date, boatClass }),
    enabled: clubReady && date !== "",
    staleTime: 15_000,
  });

  const consents = useQuery({
    queryKey: ["member", "consents"],
    queryFn: () => api.member.consents(),
    staleTime: 60_000,
  });

  const bookingConsents = (consents.data ?? []).filter((c) => c.scope === "Booking");
  const missingRequiredConsents = useMemo(
    () => bookingConsents.filter((c) => c.required && !c.accepted && !acceptedConsents.includes(c.key)),
    [bookingConsents, acceptedConsents],
  );

  const myPackages = useQuery({ queryKey: ["member", "packages"], queryFn: () => api.member.packages() });
  const hasPackageCredit = (myPackages.data ?? []).some((p) => p.remainingSessions > 0);

  const book = useMutation({
    mutationFn: () =>
      api.member.book({
        date,
        time: selectedSlot ?? "",
        boatClass,
        note: note.trim() === "" ? null : note.trim(),
        reminderMinutes:
          reminderMinutes ?? member?.defaultReminderMinutes ?? options.data?.defaultReminderMinutes ?? null,
        usePackage,
        acceptedConsents,
      }),
    onSuccess: async (response) => {
      setConfirmation(response);
      setSelectedSlot(null);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["member", "appointments"] }),
        queryClient.invalidateQueries({ queryKey: ["public", subdomain, "availability"] }),
        queryClient.invalidateQueries({ queryKey: ["member", "packages"] }),
        queryClient.invalidateQueries({ queryKey: ["public", subdomain, "consents"] }),
      ]);
    },
    onError: (error) => toastError(error instanceof Error ? error.message : "Rezervasyon alınamadı."),
  });

  if (!clubReady) {
    return (
      <div className="rounded-lg border border-warning/30 bg-warning-bg p-6 text-center">
        <p className="font-display text-base font-bold text-warning">Kulüp bağlamı bulunamadı</p>
        <p className="mt-2 text-sm text-ink-2">
          Rezervasyon için kulüp bağlamı gerekiyor. Çıkış yapıp kulübünüzün adresinden yeniden giriş yapın.
        </p>
      </div>
    );
  }

  if (confirmation !== null) {
    return (
      <div className="cb-rise rounded-lg border border-success/30 bg-success-bg p-6 text-center">
        <p className="font-display text-lg font-extrabold text-success">Randevunuz alındı 🎉</p>
        <p className="mt-2 text-sm text-ink tnum">
          {formatIsoDateTr(confirmation.date)} {confirmation.startTime} ·{" "}
          {BOAT_CLASS_LABELS[confirmation.boatClass] ?? confirmation.boatClass}
          {confirmation.boatName !== null ? ` · ${confirmation.boatName}` : ""}
          {confirmation.instructorName !== null ? ` · ${confirmation.instructorName}` : ""}
        </p>
        <Button variant="secondary" size="sm" className="mt-4" onClick={() => setConfirmation(null)}>
          Yeni rezervasyon
        </Button>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-5">
      <NextSessionCard />

      <section aria-label="Tarih ve tekne sınıfı" className="flex flex-col gap-3 rounded-lg border border-line/80 bg-surface p-4 shadow-xs">
        <div className="flex items-center justify-between gap-2">
          <h2 className="text-sm font-semibold text-ink">Ne zaman?</h2>
          <label className="text-xs text-ink-3">
            <input
              type="date"
              value={date}
              min={todayIso()}
              max={addDaysIso(todayIso(), options.data?.maxAdvanceDays ?? 14)}
              onChange={(e) => {
                if (e.target.value !== "") setDate(e.target.value);
                setSelectedSlot(null);
              }}
              aria-label="Tarih"
              className="ml-2 h-8 rounded-sm border border-line px-2 text-sm"
            />
          </label>
        </div>
        {options.data !== undefined ? (
          <>
            <p className="text-xs text-ink-3">
              Çalışma saatleri: {options.data.openingTime}–{options.data.closingTime}
            </p>
            <fieldset className="flex flex-wrap gap-1.5" aria-label="Tekne sınıfı">
              {(options.data.boatClasses).map((option) => (
                <button
                  key={option.value}
                  type="button"
                  aria-pressed={boatClass === option.value}
                  onClick={() => {
                    setBoatClass(option.value);
                    setSelectedSlot(null);
                  }}
                  className={[
                    "rounded-md border px-3 py-1.5 text-[13px] transition-colors",
                    boatClass === option.value
                      ? "border-brand-600 bg-brand-600 text-white"
                      : "border-line bg-surface text-ink-2 hover:border-line-strong",
                  ].join(" ")}
                >
                  {option.label}
                </button>
              ))}
            </fieldset>
          </>
        ) : null}
      </section>

      <section aria-label="Saat seçimi" className="flex flex-col gap-2">
        <h2 className="text-sm font-semibold text-ink">Saat seç</h2>
        <AsyncBoundary state={availability} isEmpty={(availability.data?.length ?? 0) === 0}>
          {(availability.data?.length ?? 0) === 0 ? (
            <EmptyState title="Bu gün için slot yok" description="Kulüp kapalı olabilir; başka bir tarih seçin." />
          ) : (
            <div className="grid grid-cols-4 gap-2 sm:grid-cols-6">
              {(availability.data ?? []).map((slot) => {
                const selected = selectedSlot === slot.time;
                return (
                  <button
                    key={slot.time}
                    type="button"
                    disabled={!slot.available || book.isPending}
                    aria-pressed={selected}
                    onClick={() => setSelectedSlot(slot.time)}
                    className={[
                      "flex flex-col items-center rounded-md border px-2 py-2 text-center transition-colors",
                      selected
                        ? "border-brand-600 bg-brand-600 text-white"
                        : slot.available
                          ? "border-line bg-surface text-ink hover:border-brand-400"
                          : "cursor-not-allowed border-line bg-surface-2 text-ink-3 line-through opacity-60",
                    ].join(" ")}
                  >
                    <span className="font-mono text-[13px]">{slot.time}</span>
                    <span className="text-[10px]">{slot.available ? `${slot.seatsLeft} yer` : "dolu"}</span>
                  </button>
                );
              })}
            </div>
          )}
        </AsyncBoundary>
      </section>

      {selectedSlot !== null && missingRequiredConsents.length > 0 ? (
        <section aria-label="Zorunlu beyanlar" className="rounded-lg border border-warning/40 bg-warning-bg p-4">
          <h2 className="mb-2 text-sm font-semibold text-warning">Devam etmek için onaylayın</h2>
          <ul className="flex flex-col gap-2">
            {missingRequiredConsents.map((consent) => (
              <li key={consent.key}>
                <details>
                  <summary className="cursor-pointer text-[13px] text-ink">
                    {consent.icon} {consent.title}
                  </summary>
                  <p className="mt-1 pl-6 text-xs text-ink-2">{consent.body}</p>
                </details>
                <label className="mt-1 flex items-center gap-2 pl-6 text-[13px]">
                  <input
                    type="checkbox"
                    className="size-4 accent-brand-600"
                    checked={acceptedConsents.includes(consent.key)}
                    onChange={(e) =>
                      setAcceptedConsents((prev) =>
                        e.target.checked ? [...prev, consent.key] : prev.filter((k) => k !== consent.key),
                      )
                    }
                  />
                  Okudum, onaylıyorum
                </label>
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      <section aria-label="Rezervasyon özeti" className="flex flex-col gap-3 rounded-lg border border-line bg-surface p-4 shadow-xs">
        <h2 className="text-sm font-semibold text-ink">Özet</h2>
        <dl className="grid grid-cols-2 gap-x-4 gap-y-1 text-[13px]">
          <dt className="text-ink-3">Tarih</dt>
          <dd>{formatIsoDateTr(date)} {weekdayShort(date)}</dd>
          <dt className="text-ink-3">Saat</dt>
          <dd>{selectedSlot ?? "—"}</dd>
          <dt className="text-ink-3">Sınıf</dt>
          <dd>{BOAT_CLASS_LABELS[boatClass as keyof typeof BOAT_CLASS_LABELS] ?? boatClass}</dd>
          <dt className="text-ink-3">Seviyeniz</dt>
          <dd>{rowingLevelLabel(member?.level ?? 0)}</dd>
        </dl>
        <FormField label="Not (opsiyonel)">
          {(id) => (
            <input
              id={id}
              value={note}
              onChange={(e) => setNote(e.target.value)}
              maxLength={1000}
              className="h-9 w-full rounded-md border border-line px-3 text-sm"
              placeholder="örn. İlk dersim"
            />
          )}
        </FormField>
        {options.data !== undefined && options.data.reminderOptions.length > 0 ? (
          <FormField label="Hatırlatma">
            {(id) => (
              <select
                id={id}
                value={String(reminderMinutes ?? member?.defaultReminderMinutes ?? options.data?.defaultReminderMinutes ?? 0)}
                onChange={(e) => setReminderMinutes(Number(e.target.value))}
                className="h-9 w-full rounded-md border border-line px-3 text-sm sm:w-auto"
              >
                <option value="0">Hatırlatma yok</option>
                {options.data?.reminderOptions.map((minutes) => (
                  <option key={minutes} value={String(minutes)}>
                    {minutes} dk önce
                  </option>
                ))}
              </select>
            )}
          </FormField>
        ) : null}
        {hasPackageCredit ? (
          <label className="flex items-center gap-2 text-[13px] text-ink-2">
            <input
              type="checkbox"
              className="size-4 accent-brand-600"
              checked={usePackage}
              onChange={(e) => setUsePackage(e.target.checked)}
            />
            Paket dersinden düşülsün
            {myPackages.data !== undefined ? (
              <Badge tone="info">
                bakiye {myPackages.data.reduce((sum, p) => sum + p.remainingSessions, 0)}
              </Badge>
            ) : null}
          </label>
        ) : null}

        {book.isError ? (
          <p role="alert" className="rounded-md bg-danger-bg px-3 py-2 text-sm text-danger">
            {book.error instanceof Error ? book.error.message : "Rezervasyon alınamadı."}
          </p>
        ) : null}

        <Button
          pending={book.isPending}
          disabled={selectedSlot === null || missingRequiredConsents.length > 0}
          onClick={() => book.mutate()}
        >
          Rezervasyonu tamamla
        </Button>
      </section>
    </div>
  );
}
