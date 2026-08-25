import { useState } from "react";
import { QueryClient, QueryClientProvider, useMutation, useQuery } from "@tanstack/react-query";
import type { BookAppointmentResponse } from "@crewbase/api-types";
import { AsyncBoundary, BOAT_CLASS_LABELS, Button, FormField, Input, PennantStrip, ThemeToggle } from "@crewbase/design-system";
import { persistClub, resolveSubdomain, usePublicApi } from "./app/api-context";

export default function Root() {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: { queries: { retry: false, staleTime: 60_000 }, mutations: { retry: false } },
      }),
  );

  const [club, setClub] = useState<string | null>(() => resolveSubdomain());

  if (club === null) {
    return <ClubEntryScreen onConfirm={(sub) => { persistClub(sub); setClub(sub); }} />;
  }

  return (
    <QueryClientProvider client={queryClient}>
      <ClubPage subdomain={club} />
    </QueryClientProvider>
  );
}

/** Guided dev entry: user supplies their own club subdomain; backend still validates it. */
function ClubEntryScreen({ onConfirm }: { readonly onConfirm: (subdomain: string) => void }) {
  const [value, setValue] = useState("");
  return (
    <main className="flex min-h-dvh items-center justify-center bg-canvas px-4">
      <div className="w-full max-w-md rounded-xl border border-line/80 bg-surface p-6 shadow-md sm:p-8">
        <div className="mb-5 flex items-center gap-2.5">
          <span
            aria-hidden="true"
            className="flex size-9 items-center justify-center rounded-lg bg-linear-to-br from-brand-400 to-brand-600 shadow-xs"
          >
            <svg width="19" height="19" viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <path
                d="M3 16c2.2 0 2.2-1.8 4.5-1.8S9.7 16 12 16s2.3-1.8 4.5-1.8S19.8 16 21 16M6 11l6-7 6 7"
                stroke="#fff"
                strokeWidth="1.9"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </svg>
          </span>
          <span className="font-display text-base font-extrabold tracking-[-0.01em] text-ink">CrewBase</span>
        </div>
        <h1 className="font-display text-xl font-extrabold tracking-[-0.02em] text-ink">Kulübünüze ulaşın</h1>
        <p className="mt-1.5 text-sm leading-relaxed text-ink-2">
          Üretimde kulüpler <code className="font-mono text-[13px]">*.faturebase.com</code> adresinden yayınlanır.
          Yerel geliştirmede kulüp alt alan adını buraya girin — seçiminiz bu tarayıcıda hatırlanır.
        </p>
        <form
          className="mt-5 flex items-end gap-2"
          onSubmit={(e) => {
            e.preventDefault();
            const trimmed = value.trim().toLowerCase();
            if (trimmed !== "") onConfirm(trimmed);
          }}
          noValidate
        >
          <FormField label="Kulüp alt alan adı" required className="flex-1">
            {(id) => (
              <Input
                id={id}
                value={value}
                onChange={(e) => setValue(e.target.value)}
                placeholder="kulupadi"
                autoComplete="off"
              />
            )}
          </FormField>
          <Button type="submit" disabled={value.trim() === ""}>
            Git
          </Button>
        </form>
      </div>
    </main>
  );
}

function ClubPage({ subdomain }: { readonly subdomain: string }) {
  const info = useInfo(subdomain);
  const options = useOptions(subdomain);
  const consents = useConsents(subdomain);

  return (
    <div className="min-h-dvh">
      {/* ── Hero ─────────────────────────────────────────── */}
      <div className="relative overflow-hidden bg-rail pb-16 pt-10 sm:pb-24 sm:pt-14">
        <div className="absolute right-4 top-4 z-10">
          <ThemeToggle />
        </div>
        {/* Cinematic animated mesh */}
        <div
          aria-hidden="true"
          className="mesh-a pointer-events-none absolute -top-[28rem] right-[-14%] size-[46rem] rounded-full opacity-50"
          style={{ background: "radial-gradient(circle at center, rgb(64 144 196 / 0.5) 0%, transparent 62%)" }}
        />
        <div
          aria-hidden="true"
          className="mesh-b pointer-events-none absolute -bottom-[30rem] left-[-16%] size-[42rem] rounded-full opacity-35"
          style={{ background: "radial-gradient(circle at center, rgb(189 102 49 / 0.5) 0%, transparent 62%)" }}
        />
        <PennantStrip count={20} className="relative mx-auto mb-8 w-full max-w-3xl text-white/50" />
        <svg
          aria-hidden="true"
          className="pointer-events-none absolute inset-x-0 bottom-0 h-24 w-full opacity-[0.14]"
          viewBox="0 0 600 120"
          fill="none"
          preserveAspectRatio="none"
        >
          {[0, 1, 2].map((row) => (
            <path
              key={row}
              d={`M0 ${30 + row * 28} C 100 ${16 + row * 28}, 180 ${44 + row * 28}, 300 ${30 + row * 28} S 500 ${16 + row * 28}, 600 ${30 + row * 28}`}
              stroke="#ffffff"
              strokeWidth="1.3"
            />
          ))}
        </svg>

        <div className="relative mx-auto max-w-3xl px-4 text-center">
          <AsyncBoundary state={info}>
            {info.data !== undefined ? (
              <>
                <p className="text-[11px] font-bold uppercase tracking-[0.18em] text-rail-text">CrewBase kulübü</p>
                <h1 className="font-display mt-3 text-[38px] font-extrabold leading-[1.08] tracking-[-0.03em] text-white drop-shadow-[0_2px_14px_rgb(0_0_0/0.35)] sm:text-[54px]">
                  {info.data.name}
                </h1>
                <ul className="mt-4 flex flex-wrap items-center justify-center gap-2 text-sm">
                  {info.data.phone !== null ? <ContactPill>{`☎ ${info.data.phone}`}</ContactPill> : null}
                  {info.data.contactEmail !== null ? <ContactPill>{`✉ ${info.data.contactEmail}`}</ContactPill> : null}
                  {info.data.address !== null ? <ContactPill>{`📍 ${info.data.address}`}</ContactPill> : null}
                </ul>
                <a
                  href="#rezervasyon"
                  className="mt-6 inline-flex items-center gap-2 rounded-full bg-white px-5 py-2.5 text-sm font-bold tracking-[-0.01em] text-rail shadow-sm transition-all duration-200 hover:-translate-y-0.5 hover:shadow-md"
                >
                  Rezervasyona başla
                  <span aria-hidden="true">↓</span>
                </a>
              </>
            ) : (
              <span className="inline-block h-10 w-56 animate-pulse rounded-md bg-white/10" />
            )}
          </AsyncBoundary>
        </div>
      </div>

      {/* ── Content ──────────────────────────────────────── */}
      <div className="relative mx-auto -mt-8 flex max-w-3xl flex-col gap-6 px-4 pb-14">
        {options.data !== undefined ? (
          <section aria-label="Kulüp bilgileri" className="grid gap-3 sm:grid-cols-3">
            <InfoTile title="Çalışma saatleri" value={`${options.data.openingTime}–${options.data.closingTime}`} />
            <InfoTile title="Seans süresi" value={`${options.data.slotMinutes} dk`} />
            <InfoTile
              title="Tekne sınıfları"
              value={options.data.boatClasses.map((c) => `${c.label} (${c.capacity})`).join(" · ")}
            />
          </section>
        ) : null}

      <BookingWizard
        subdomain={subdomain}
        consents={(consents.data ?? []).filter((c) => c.scope === "Booking")}
        reminderOptions={options.data?.reminderOptions ?? []}
        maxAdvanceDays={options.data?.maxAdvanceDays ?? 14}
      />

        <footer className="pt-4 text-center text-xs text-ink-3">
          <a href="/" className="hover:text-ink-2">CrewBase</a> · Rezervasyonlar kulübün çalışma kurallarına göre yapılır.
        </footer>
      </div>
    </div>
  );
}

function ContactPill({ children }: { readonly children: React.ReactNode }) {
  return (
    <li className="rounded-full border border-white/20 bg-white/10 px-3.5 py-1 font-medium text-white/90 backdrop-blur-sm">
      {children}
    </li>
  );
}

function InfoTile({ title, value }: { readonly title: string; readonly value: string }) {
  return (
    <div className="rounded-lg border border-line/80 bg-surface p-4 shadow-sm transition-shadow duration-200 hover:shadow-md">
      <p className="text-[11px] font-bold uppercase tracking-[0.07em] text-ink-3">{title}</p>
      <p className="mt-1 text-sm font-semibold text-ink">{value}</p>
    </div>
  );
}

interface ConsentRow {
  readonly key: string;
  readonly title: string;
  readonly body: string;
  readonly required: boolean;
  readonly icon: string;
}

function BookingWizard({
  subdomain,
  consents,
  reminderOptions,
  maxAdvanceDays,
}: {
  readonly subdomain: string;
  readonly consents: readonly ConsentRow[];
  readonly reminderOptions: readonly number[];
  readonly maxAdvanceDays: number;
}) {
  const api = usePublicApi();

  const [step, setStep] = useState<"slot" | "identity">("slot");
  const todayIsoValue = (() => {
    const now = new Date();
    return `${now.getFullYear()}-${`${now.getMonth() + 1}`.padStart(2, "0")}-${`${now.getDate()}`.padStart(2, "0")}`;
  })();
  const maxDateIso = (() => {
    const d = new Date();
    d.setDate(d.getDate() + Math.max(0, maxAdvanceDays));
    return `${d.getFullYear()}-${`${d.getMonth() + 1}`.padStart(2, "0")}-${`${d.getDate()}`.padStart(2, "0")}`;
  })();
  const [date, setDate] = useState(todayIsoValue);
  const [boatClass, setBoatClass] = useState("4x");
  const [selectedSlot, setSelectedSlot] = useState<string | null>(null);

  const [fullName, setFullName] = useState("");
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");
  const [note, setNote] = useState("");
  const [reminderMinutes, setReminderMinutes] = useState<number | null>(null);
  const [accepted, setAccepted] = useState<string[]>([]);
  const [confirmation, setConfirmation] = useState<BookAppointmentResponse | null>(null);

  // Step 1: anonymous availability (level 0).
  // Step 2: re-check with the entered phone so existing members see their true level slots.
  const availabilityStep1 = useAvailability(subdomain, date, boatClass, step === "slot" ? undefined : undefined);
  const phoneQuery = phone.trim() === "" ? undefined : phone.trim();
  const availabilityWithPhone = useAvailability(subdomain, date, boatClass, step === "identity" ? phoneQuery : undefined);
  const slots = step === "slot" ? (availabilityStep1.data ?? []) : (availabilityWithPhone.data ?? []);

  const requiredConsents = consents.filter((c) => c.required);
  const missingConsents = requiredConsents.filter((c) => !accepted.includes(c.key));

  const book = useMutation({
    mutationFn: () =>
      api.bookAppointment(subdomain, {
        fullName: fullName.trim(),
        phone: phone.trim(),
        email: email.trim() === "" ? null : email.trim(),
        date,
        time: selectedSlot ?? "",
        boatClass,
        note: note.trim() === "" ? null : note.trim(),
        reminderMinutes,
        acceptedConsents: accepted,
      }),
    onSuccess: (response) => setConfirmation(response),
  });

  if (confirmation !== null) {
    return (
      <section aria-label="Rezervasyon onayı" className="rounded-lg border border-success/30 bg-success-bg p-6 text-center">
        <p className="font-display text-xl font-semibold text-success">Randevunuz alındı!</p>
        <p className="mt-2 text-sm text-ink">
          {confirmation.date} {confirmation.startTime} ·{" "}
          {BOAT_CLASS_LABELS[confirmation.boatClass] ?? confirmation.boatClass}
          {confirmation.boatName !== null ? ` · ${confirmation.boatName}` : ""}
          {confirmation.instructorName !== null ? ` · Eğitmen ${confirmation.instructorName}` : ""}
        </p>
        <p className="mt-1 text-xs text-ink-3">
          Rezervasyon no: <span className="font-mono">{confirmation.appointmentId.slice(0, 8)}</span>
        </p>
      </section>
    );
  }

  return (
    <section
      id="rezervasyon"
      aria-label="Rezervasyon"
      className="flex scroll-mt-6 flex-col gap-5 rounded-xl border border-line/80 bg-surface p-5 shadow-md sm:p-7"
    >
      <ol className="flex items-center gap-2 text-[13px]" aria-label="Adımlar">
        {[step === "slot" ? "1" : "✓", "2"].map((mark, index) => {
          const active = (index === 0 && step === "slot") || (index === 1 && step === "identity");
          return (
            <li key={index} className="flex items-center gap-2">
              {index > 0 ? <span aria-hidden="true" className="text-line-strong">──</span> : null}
              <span
                className={[
                  "flex size-6 items-center justify-center rounded-full font-display text-[11px] font-extrabold",
                  active
                    ? "bg-brand-600 text-white shadow-xs"
                    : "border border-line-strong/70 bg-surface text-ink-3",
                ].join(" ")}
                aria-current={active ? "step" : undefined}
              >
                {mark}
              </span>
              <span className={active ? "font-semibold text-brand-700" : "font-medium text-ink-3"}>
                {index === 0 ? "Slot seç" : "Bilgiler"}
              </span>
            </li>
          );
        })}
      </ol>

      <div className="flex flex-wrap items-center gap-3">
        <label className="text-[13px] font-medium text-ink-2">
          Tarih
          <input
            type="date"
            value={date}
            min={todayIsoValue}
            max={maxDateIso}
            onChange={(e) => {
              if (e.target.value !== "") {
                setDate(e.target.value);
                setSelectedSlot(null);
              }
            }}
            aria-label="Rezervasyon tarihi"
            className="ml-2 h-9 rounded-md border border-line bg-surface px-2 text-sm"
          />
        </label>
      </div>

      <fieldset className="flex flex-wrap gap-1.5" aria-label="Tekne sınıfı">
        {(["1x", "2x", "4x"] as const).map((cls) => (
          <button
            key={cls}
            type="button"
            aria-pressed={boatClass === cls}
            onClick={() => {
              setBoatClass(cls);
              setSelectedSlot(null);
            }}
            title={cls !== "4x" ? "Misafir rezervasyonları için önerilen sınıf 4x'tir." : undefined}
            className={[
              "rounded-md border px-3 py-1.5 text-[13px] transition-colors",
              boatClass === cls
                ? "border-brand-600 bg-brand-600 text-white"
                : "border-line bg-surface text-ink-2 hover:border-line-strong",
            ].join(" ")}
          >
            {cls}
          </button>
        ))}
      </fieldset>
      <p className="-mt-3 text-xs text-ink-3">
        Hesabınız yoksa misafir rezervasyonları yalnızca 4x sınıfına açıktır; üye girişiyle tüm sınıflar kullanılabilir.
        Telefon numaranız daha önce kayıt olduysanız derecenize uygun seanslara yönlendirilirsiniz.
      </p>

      {step === "slot" ? (
        <>
          <AsyncBoundary state={availabilityStep1} isEmpty={slots.length === 0}>
            {slots.length === 0 ? (
              <p className="rounded-md border border-dashed border-line px-4 py-8 text-center text-sm text-ink-3">
                Bu gün için müsait slot yok.
              </p>
            ) : (
              <div className="grid grid-cols-4 gap-2 sm:grid-cols-6">
                {slots.map((slot) => (
                  <button
                    key={slot.time}
                    type="button"
                    disabled={!slot.available}
                    aria-pressed={selectedSlot === slot.time}
                    onClick={() => setSelectedSlot(slot.time)}
                    className={[
                      "flex flex-col items-center rounded-md border px-2 py-2 transition-colors",
                      selectedSlot === slot.time
                        ? "border-brand-600 bg-brand-600 text-white"
                        : slot.available
                          ? "border-line text-ink hover:border-brand-400"
                          : "cursor-not-allowed border-line bg-surface-2 text-ink-3 line-through opacity-60",
                    ].join(" ")}
                  >
                    <span className="font-mono text-[13px]">{slot.time}</span>
                    <span className="text-[10px]">{slot.available ? `${slot.seatsLeft} yer` : "dolu"}</span>
                  </button>
                ))}
              </div>
            )}
          </AsyncBoundary>

          <Button disabled={selectedSlot === null} onClick={() => setStep("identity")}>
            Devam et
          </Button>
        </>
      ) : (
        <>
          <form
            className="flex flex-col gap-4"
            onSubmit={(e) => {
              e.preventDefault();
              if (missingConsents.length === 0 && fullName.trim() !== "" && phone.trim() !== "") book.mutate();
            }}
            noValidate
          >
            <div className="grid gap-4 sm:grid-cols-2">
              <FormField label="Ad Soyad" required error={fullName.trim() === "" && book.isError ? "Zorunludur." : undefined}>
                {(id) => <Input id={id} value={fullName} onChange={(e) => setFullName(e.target.value)} maxLength={200} />}
              </FormField>
              <FormField label="Telefon" required hint="Daha önce randevu aldıysanız derecenizle eşleştirilirsiniz.">
                {(id) => <Input id={id} type="tel" value={phone} onChange={(e) => setPhone(e.target.value)} maxLength={20} />}
              </FormField>
            </div>
            <FormField label="E-posta (opsiyonel)">
              {(id) => <Input id={id} type="email" value={email} onChange={(e) => setEmail(e.target.value)} maxLength={254} />}
            </FormField>
            <FormField label="Not (opsiyonel)">
              {(id) => <Input id={id} value={note} onChange={(e) => setNote(e.target.value)} maxLength={1000} />}
            </FormField>

            {reminderOptions.length > 0 ? (
              <FormField label="Hatırlatma">
                {(id) => (
                  <select
                    id={id}
                    value={String(reminderMinutes ?? 0)}
                    onChange={(e) => setReminderMinutes(Number(e.target.value))}
                    className="h-10 w-full rounded-md border border-line bg-surface px-3 text-sm sm:w-56"
                  >
                    <option value="0">Hatırlatma yok</option>
                    {reminderOptions.map((minutes) => (
                      <option key={minutes} value={String(minutes)}>
                        {minutes} dk önce
                      </option>
                    ))}
                  </select>
                )}
              </FormField>
            ) : null}

            <fieldset className="flex flex-col gap-3 rounded-md border border-line p-4">
              <legend className="px-1 text-[13px] font-medium text-ink-2">Beyanlar</legend>
              {consents.length === 0 ? (
                <p className="text-sm text-ink-3">Beyanlar yükleniyor…</p>
              ) : null}
              {consents.map((consent) => (
                <label key={consent.key} className="flex items-start gap-2.5">
                  <input
                    type="checkbox"
                    className="mt-1 size-4 accent-brand-600"
                    checked={accepted.includes(consent.key)}
                    onChange={(e) =>
                      setAccepted((prev) =>
                        e.target.checked ? [...prev, consent.key] : prev.filter((k) => k !== consent.key),
                      )
                    }
                  />
                  <span>
                    <span className="text-[13px] font-medium text-ink">
                      {consent.icon} {consent.title}
                      {consent.required ? " *" : ""}
                    </span>
                    <span className="block text-xs leading-5 text-ink-3">{consent.body}</span>
                  </span>
                </label>
              ))}
            </fieldset>

            {book.isError ? (
              <p role="alert" className="rounded-md bg-danger-bg px-3 py-2 text-sm text-danger">
                {book.error instanceof Error ? book.error.message : "Rezervasyon alınamadı."}
              </p>
            ) : null}
            {missingConsents.length > 0 ? (
              <p role="alert" className="text-xs font-medium text-warning">
                Devam etmek için tüm zorunlu beyanları onaylayın.
              </p>
            ) : null}

            <div className="flex gap-2">
              <Button variant="secondary" onClick={() => setStep("slot")}>
                Geri
              </Button>
              <Button
                type="submit"
                pending={book.isPending}
                disabled={selectedSlot === null || missingConsents.length > 0 || fullName.trim() === "" || phone.trim() === ""}
              >
                Rezervasyonu tamamla
              </Button>
            </div>
          </form>
          <p className="text-xs text-ink-3">
            Seçim: {date} {selectedSlot ?? ""} · {boatClass}
          </p>
        </>
      )}
    </section>
  );
}

function useInfo(subdomain: string | null) {
  const api = usePublicApi();
  return useQuery({
    queryKey: ["public", subdomain ?? "", "info"],
    queryFn: () => api.clubInfo(subdomain ?? ""),
    enabled: subdomain !== null,
    retry: false,
    staleTime: 5 * 60_000,
  });
}

function useOptions(subdomain: string | null) {
  const api = usePublicApi();
  return useQuery({
    queryKey: ["public", subdomain ?? "", "options"],
    queryFn: () => api.options(subdomain ?? ""),
    enabled: subdomain !== null,
    staleTime: 10 * 60_000,
  });
}

function useConsents(subdomain: string | null) {
  const api = usePublicApi();
  return useQuery({
    queryKey: ["public", subdomain ?? "", "consents"],
    queryFn: () => api.consents(subdomain ?? ""),
    enabled: subdomain !== null,
    staleTime: 10 * 60_000,
  });
}

function useAvailability(
  subdomain: string | null,
  date: string,
  boatClass: string,
  phone?: string,
) {
  const api = usePublicApi();
  return useQuery({
    queryKey: ["public", subdomain ?? "", "availability", date, boatClass, phone ?? null],
    queryFn: () => api.availability(subdomain ?? "", { date, boatClass, phone }),
    enabled: subdomain !== null,
    staleTime: 15_000,
  });
}
