import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { CompanySettingsDto } from "@crewbase/api-types";
import {
  AsyncBoundary,
  Badge,
  Button,
  FormField,
  Input,
  PageHeader,
  SectionCard,
  StickyActionBar,
  TimelineList,
  TimelineRow,
  WEEKDAY_SHORT_LABELS,
} from "@crewbase/design-system";
import { useApi } from "../../app/api-context";
import { panelKeys } from "../../shared/query-keys";
import { useSessionStore } from "../auth/session-store";
import { formatIsoDateTr, todayIso } from "../../shared/dates";
import { toastError, toastSuccess } from "../../shared/toast-store";

const HHMM = /^([01]\d|2[0-3]):[0-5]\d$/;

export function SettingsPage() {
  return (
    <div className="flex flex-col gap-6">
      <PageHeader title="Ayarlar" description="Çalışma saatleri, rezervasyon kuralları ve kulüp sitesi." />
      <SiteCard />
      <SettingsForm />
      <ClosedDates />
    </div>
  );
}

function SiteCard() {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const site = useQuery({ queryKey: panelKeys.site(companyId), queryFn: () => api.company.site(), staleTime: 5 * 60_000 });

  if (site.data === undefined) return null;
  return (
    <SectionCard title="Kulüp sitesi" description="Üyelerinizin rezervasyon yaptığı genel adres.">
      <div className="flex flex-wrap items-center gap-2 text-sm">
        <span className="font-mono text-brand-700">{site.data.siteUrl}</span>
        <a href={site.data.siteUrl} target="_blank" rel="noreferrer" className="text-xs text-brand-600 hover:underline">
          siteyi aç ↗
        </a>
        <Badge tone="neutral">/site/{site.data.subdomain}</Badge>
      </div>
    </SectionCard>
  );
}

function SettingsForm() {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const queryClient = useQueryClient();

  const settings = useQuery({ queryKey: panelKeys.settings(companyId), queryFn: () => api.company.settings() });

  const [values, setValues] = useState<CompanySettingsDto | null>(null);

  useEffect(() => {
    if (settings.data !== undefined && values === null) setValues(settings.data);
  }, [settings.data, values]);

  const save = useMutation({
    mutationFn: () => {
      if (values === null) throw new Error("Ayarlar yüklenmedi.");
      if (!HHMM.test(values.openingTime) || !HHMM.test(values.closingTime)) {
        throw new Error("Saatler SS:DD biçiminde olmalıdır.");
      }
      if (values.openDays.length === 0) throw new Error("En az bir açık gün seçin.");
      return api.company.updateSettings(values);
    },
    onSuccess: async (saved) => {
      setValues(saved);
      await queryClient.invalidateQueries({ queryKey: panelKeys.settings(companyId) });
      toastSuccess("Ayarlar güncellendi.");
    },
    onError: (error) => toastError(error instanceof Error ? error.message : "Ayarlar kaydedilemedi."),
  });

  const dirty =
    values !== null &&
    settings.data !== undefined &&
    JSON.stringify(values) !== JSON.stringify(settings.data);

  function toggleDay(day: number) {
    setValues((current) =>
      current === null
        ? current
        : {
            ...current,
            openDays: current.openDays.includes(day)
              ? current.openDays.filter((d) => d !== day)
              : [...current.openDays, day].sort((a, b) => a - b),
          },
    );
  }

  function toggleReminder(minutes: number) {
    setValues((current) =>
      current === null
        ? current
        : {
            ...current,
            reminderOptions: current.reminderOptions.includes(minutes)
              ? current.reminderOptions.filter((m) => m !== minutes)
              : [...current.reminderOptions, minutes].sort((a, b) => a - b),
          },
    );
  }

  return (
    <>
      <SectionCard title="Rezervasyon kuralları" description="Bu kurallar genel site ve üye akışlarını doğrudan belirler.">
      <AsyncBoundary state={settings}>
        {values !== null ? (
          <form className="flex flex-col gap-6" onSubmit={(e) => { e.preventDefault(); save.mutate(); }} noValidate>
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              <FormField label="Açılış saati" required>
                {(id) => (
                  <Input id={id} type="time" value={values.openingTime} onChange={(e) => setValues({ ...values, openingTime: e.target.value })} />
                )}
              </FormField>
              <FormField label="Kapanış saati" required>
                {(id) => (
                  <Input id={id} type="time" value={values.closingTime} onChange={(e) => setValues({ ...values, closingTime: e.target.value })} />
                )}
              </FormField>
              <FormField label="Slot süresi (dk)" required hint="Seans uzunluğu">
                {(id) => (
                  <Input
                    id={id}
                    inputMode="numeric"
                    value={String(values.slotMinutes)}
                    onChange={(e) => setValues({ ...values, slotMinutes: Number(e.target.value) || 0 })}
                  />
                )}
              </FormField>
              <FormField label="Saat dilimi" required hint="IANA adı — örn. Europe/Istanbul">
                {(id) => (
                  <>
                    <Input id={id} list="tz-options" value={values.timeZoneId} onChange={(e) => setValues({ ...values, timeZoneId: e.target.value })} />
                    <datalist id="tz-options">
                      <option value="Europe/Istanbul" />
                      <option value="Europe/London" />
                      <option value="Europe/Berlin" />
                      <option value="UTC" />
                    </datalist>
                  </>
                )}
              </FormField>
            </div>

            <fieldset className="flex flex-col gap-2">
              <legend className="text-[13px] font-medium text-ink-2">Açık günler</legend>
              <div className="flex flex-wrap gap-1.5">
                {[0, 1, 2, 3, 4, 5, 6].map((day) => (
                  <button
                    key={day}
                    type="button"
                    aria-pressed={values.openDays.includes(day)}
                    onClick={() => toggleDay(day)}
                    className={[
                      "rounded-md border px-3 py-1.5 text-[13px] transition-colors",
                      values.openDays.includes(day)
                        ? "border-brand-600 bg-brand-600 text-white"
                        : "border-line bg-surface text-ink-2 hover:border-line-strong",
                    ].join(" ")}
                  >
                    {WEEKDAY_SHORT_LABELS[day]}
                  </button>
                ))}
              </div>
            </fieldset>

            <div className="grid gap-4 sm:grid-cols-2">
              <FormField label="Son rezervasyon saati (randevudan en az X saat önce)" required>
                {(id) => (
                  <Input
                    id={id}
                    inputMode="numeric"
                    value={String(values.minNoticeHours)}
                    onChange={(e) => setValues({ ...values, minNoticeHours: Number(e.target.value) || 0 })}
                  />
                )}
              </FormField>
              <FormField label="Maksimum ileri tarih (gün)" required>
                {(id) => (
                  <Input
                    id={id}
                    inputMode="numeric"
                    value={String(values.maxAdvanceDays)}
                    onChange={(e) => setValues({ ...values, maxAdvanceDays: Number(e.target.value) || 0 })}
                  />
                )}
              </FormField>
            </div>

            <fieldset className="flex flex-col gap-2">
              <legend className="text-[13px] font-medium text-ink-2">Hatırlatma seçenekleri (dakika)</legend>
              <div className="flex flex-wrap items-center gap-1.5">
                {[30, 60, 120, 180, 360, 720, 1440].map((minutes) => (
                  <button
                    key={minutes}
                    type="button"
                    aria-pressed={values.reminderOptions.includes(minutes)}
                    onClick={() => toggleReminder(minutes)}
                    className={[
                      "rounded-full border px-3 py-1 text-[13px] transition-colors",
                      values.reminderOptions.includes(minutes)
                        ? "border-brand-600 bg-brand-50 text-brand-700"
                        : "border-line bg-surface text-ink-2 hover:border-line-strong",
                    ].join(" ")}
                  >
                    {minutes}
                  </button>
                ))}
              </div>
              <FormField label="Varsayılan hatırlatma" hint="0 = hatırlatma istemiyorum">
                {(id) => (
                  <select
                    id={id}
                    value={String(values.defaultReminderMinutes)}
                    onChange={(e) => setValues({ ...values, defaultReminderMinutes: Number(e.target.value) })}
                    className="h-10 w-full max-w-xs rounded-md border border-line bg-surface px-3 text-sm sm:w-auto"
                  >
                    <option value="0">Hatırlatma yok</option>
                    {values.reminderOptions.map((minutes) => (
                      <option key={minutes} value={String(minutes)}>
                        {minutes} dk önce
                      </option>
                    ))}
                  </select>
                )}
              </FormField>
            </fieldset>

            <div className="flex items-center gap-2 text-xs text-ink-3">
              {dirty ? <span className="font-medium text-warning">Kaydedilmemiş değişiklikler var</span> : null}
            </div>
          </form>
        ) : null}
      </AsyncBoundary>
      </SectionCard>

      <StickyActionBar visible={dirty}>
        {dirty ? (
          <>
            <span className="mr-auto text-[13px] font-medium text-warning">Kaydedilmemiş değişiklikler</span>
            <Button variant="secondary" size="sm" onClick={() => setValues(settings.data ?? null)}>
              Vazgeç
            </Button>
            <Button size="sm" pending={save.isPending} onClick={() => save.mutate()}>
              Değişiklikleri kaydet
            </Button>
          </>
        ) : null}
      </StickyActionBar>
    </>
  );
}

function ClosedDates() {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const queryClient = useQueryClient();
  const [date, setDate] = useState("");
  const [reason, setReason] = useState("");

  const closedDates = useQuery({
    queryKey: panelKeys.closedDates(companyId),
    queryFn: () => api.company.closedDates(),
  });

  const add = useMutation({
    mutationFn: () => api.company.addClosedDate({ date, reason: reason.trim() === "" ? null : reason.trim() }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: panelKeys.closedDates(companyId) });
      setDate("");
      setReason("");
      toastSuccess("Tarih randevuya kapatıldı.");
    },
    onError: (error) => toastError(error instanceof Error ? error.message : "Tarih kapatılamadı."),
  });

  const remove = useMutation({
    mutationFn: (closedDate: string) => api.company.removeClosedDate(closedDate),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: panelKeys.closedDates(companyId) });
      toastSuccess("Tarih tekrar randevuya açıldı.");
    },
    onError: (error) => toastError(error instanceof Error ? error.message : "Tarih açılamadı."),
  });

  return (
    <SectionCard title="Kapalı günler" description="Bayram, bakım vb. randevuya kapalı günler.">
      <form className="mb-5 flex flex-wrap items-end gap-2" onSubmit={(e) => { e.preventDefault(); add.mutate(); }} noValidate>
        <label className="flex flex-col gap-1 text-[13px] text-ink-2">
          Tarih
          <input
            type="date"
            min={todayIso()}
            value={date}
            onChange={(e) => setDate(e.target.value)}
            className="h-9 rounded-sm border border-line bg-surface px-2 text-sm"
            required
          />
        </label>
        <label className="flex flex-col gap-1 text-[13px] text-ink-2">
          Sebep (opsiyonel)
          <input
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder="Resmi tatil"
            className="h-9 rounded-sm border border-line px-2 text-sm"
          />
        </label>
        <Button size="sm" pending={add.isPending} disabled={date === ""}>Günü kapat</Button>
      </form>

      <AsyncBoundary state={closedDates} isEmpty={(closedDates.data?.length ?? 0) === 0}>
        {(() => {
          const rows = closedDates.data ?? [];
          const today = todayIso();
          const upcoming = rows.filter((item) => item.date >= today);
          const past = rows.filter((item) => item.date < today).reverse();

          const renderRow = (item: (typeof rows)[number]) => (
            <TimelineRow key={item.id} dotTone={item.date >= today ? "danger" : "neutral"}>
              <div className="flex items-center justify-between gap-3">
                <span className="text-sm">
                  <span className={`font-semibold tnum ${item.date >= today ? "text-ink" : "text-ink-3"}`}>
                    {formatIsoDateTr(item.date)}
                  </span>
                  {item.reason !== null ? <span className="ml-2 text-ink-3">{item.reason}</span> : null}
                </span>
                {item.date >= today ? (
                  <Button variant="ghost" size="sm" className="text-danger" pending={remove.isPending && remove.variables === item.date} onClick={() => remove.mutate(item.date)}>
                    Yeniden aç
                  </Button>
                ) : null}
              </div>
            </TimelineRow>
          );

          return (
            <div className="flex flex-col gap-5">
              <div>
                <p className="mb-2 text-[11px] font-bold uppercase tracking-[0.07em] text-ink-3">
                  Yaklaşan ({upcoming.length})
                </p>
                {upcoming.length === 0 ? (
                  <p className="text-sm text-ink-3">Yaklaşan kapalı gün yok.</p>
                ) : (
                  <TimelineList>{upcoming.map(renderRow)}</TimelineList>
                )}
              </div>
              {past.length > 0 ? (
                <details>
                  <summary className="cursor-pointer text-[11px] font-bold uppercase tracking-[0.07em] text-ink-3">
                    Geçmiş ({past.length})
                  </summary>
                  <TimelineList className="mt-2">{past.map(renderRow)}</TimelineList>
                </details>
              ) : null}
            </div>
          );
        })()}
      </AsyncBoundary>
    </SectionCard>
  );
}
